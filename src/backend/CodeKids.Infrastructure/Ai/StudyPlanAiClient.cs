using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Ai;

/// <summary>
/// Thrown when an AI provider key hits its quota/rate limit (HTTP 429).
/// Carries the key so the caller can put it on cooldown and rotate to the next one.
/// </summary>
public sealed class AiQuotaExceededException(string apiKey, string message)
    : HttpRequestException(message)
{
    public string ApiKey { get; } = apiKey;
}

public sealed class StudyPlanAiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    IOptions<List<AiContentProviderOptions>>? contentProviders,
    IOptions<AiImageProviderChain>? imageProviders) : IStudyPlanAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private const int MaxOutputTokens = 16384;

    /// <summary>Max inline base64 length per attachment before switching to the Files API.
    /// Gemini's ~20 MB limit applies to the ENTIRE request body (prompt + all parts),
    /// so keep each attachment far below it.</summary>
    private const int MaxInlineBase64Chars = 5 * 1024 * 1024;

    /// <summary>Combined base64 budget across ALL inline attachments in one request (~10 MB of JSON headroom below Gemini's ~20 MB body limit).</summary>
    private const int TotalInlineBudgetChars = 10 * 1024 * 1024;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // Keys that returned 429 are skipped until their quota window resets.
    // Gemini's free tier resets daily, but a 1h cooldown lets keys recovered
    // early (or short-window limits) come back quickly.
    private static readonly TimeSpan QuotaCooldown = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, DateTime> QuotaCooldownUntil = new(StringComparer.Ordinal);

    private static void RegisterQuotaCooldown(string apiKey) =>
        QuotaCooldownUntil[apiKey] = DateTime.UtcNow.Add(QuotaCooldown);

    /// <summary>Filters out keys on quota cooldown; if every key is cooling down, keep them all so we still make an attempt.</summary>
    private static List<AiAttempt> FilterQuotaCooldown(List<AiAttempt> attempts)
    {
        var now = DateTime.UtcNow;
        var available = attempts.FindAll(a =>
            !QuotaCooldownUntil.TryGetValue(a.ApiKey, out var until) || until <= now);
        return available.Count > 0 ? available : attempts;
    }

    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        object? jsonSchema = null)
    {
        var attempts = BuildAttempts();
        if (attempts.Count == 0)
        {
            throw new HttpRequestException("No AI providers are configured (AiContent / Ai sections).");
        }

        attempts = FilterQuotaCooldown(attempts);

        List<Exception>? failures = null;
        for (var i = 0; i < attempts.Count; i++)
        {
            var attempt = attempts[i];
            try
            {
                var result= await CompleteWithAsync(attempt, systemPrompt, userPrompt, jsonSchema, cancellationToken);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AiQuotaExceededException ex)
            {
                RegisterQuotaCooldown(ex.ApiKey);
                (failures ??= []).Add(new HttpRequestException(
                    $"Provider '{attempt.Provider}' (model '{attempt.Model}') hit its quota; rotating to the next key.", ex));
            }
            catch (Exception ex) when (i < attempts.Count - 1)
            {
                // Try the next provider in the chain; remember why this one failed.
                (failures ??= []).Add(new HttpRequestException(
                    $"Provider '{attempt.Provider}' (model '{attempt.Model}') failed: {ex.Message}", ex));
            }
        }

        throw new AggregateException(
            "All AI providers in the fallback chain failed.", failures ?? []);
    }

    public async Task<string> CompleteJsonWithFilesAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<IStudyPlanAiClient.AiAttachment> attachments,
        CancellationToken cancellationToken,
        object? jsonSchema = null)
    {
        if (attachments.Count == 0)
        {
            return await CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken, jsonSchema);
        }

        return await CompleteFilesRetryLoopAsync(
            systemPrompt, userPrompt, attachments, jsonSchema, cancellationToken);
    }

    /// <summary>
    /// Uploads an attachment to the Gemini Files API (resumable protocol) and
    /// returns the file URI to reference in file_data parts.
    /// </summary>
    private async Task<string> UploadToGeminiFilesAsync(
        AiAttempt attempt,
        IStudyPlanAiClient.AiAttachment attachment,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));

        // 1. Ask for a resumable upload session.
        using var initRequest = new HttpRequestMessage(HttpMethod.Post,
            "https://generativelanguage.googleapis.com/upload/v1beta/files");
        initRequest.Headers.TryAddWithoutValidation("X-goog-api-key", attempt.ApiKey);
        initRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        initRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        initRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length", attachment.Data.Length.ToString());
        initRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", attachment.MimeType);
        initRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { file = new { display_name = "ssa-attachment" } }, JsonOptions),
            Utf8NoBom, "application/json");

        using var initResponse = await client.SendAsync(initRequest, cancellationToken);
        var initBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini Files API session init failed ({(int)initResponse.StatusCode}): {initBody}");
        }

        var uploadUrl = initResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var urls)
            ? urls.FirstOrDefault()
            : null;
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            throw new HttpRequestException("Gemini Files API did not return an upload URL.");
        }

        // 2. Upload the raw bytes to the session URL.
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        uploadRequest.Content = new ByteArrayContent(attachment.Data);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var uploadResponse = await client.SendAsync(uploadRequest, cancellationToken);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini Files API upload failed ({(int)uploadResponse.StatusCode}): {uploadBody}");
        }

        // 3. Pull the file URI out of the finalize response.
        using var doc = JsonDocument.Parse(uploadBody);
        if (doc.RootElement.TryGetProperty("file", out var fileEl)
            && fileEl.TryGetProperty("uri", out var uriEl))
        {
            return uriEl.GetString() ?? string.Empty;
        }

        throw new HttpRequestException("Gemini Files API upload response did not contain a file URI.");
    }

    private async Task<string> CompleteGeminiWithFilesAsync(
        AiAttempt attempt,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<IStudyPlanAiClient.AiAttachment> attachments,
        object? jsonSchema,
        CancellationToken cancellationToken,
        bool retryWithUpload = false)
    {
        var model = NormalizeGeminiModel(attempt.Model);
        var url = BuildGeminiGenerateContentUrl(attempt.BaseUrl, model);
        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("X-goog-api-key", attempt.ApiKey);

        var contentParts = await BuildGeminiContentPartsAsync(
            attempt, userPrompt, attachments, forceFileUpload: retryWithUpload, cancellationToken);

        var generationConfig = new Dictionary<string, object?>
        {
            ["responseMimeType"] = "application/json",
            ["maxOutputTokens"] = MaxOutputTokens
        };
        if (jsonSchema is not null)
        {
            generationConfig["responseJsonSchema"] = jsonSchema;
        }

        var body = new
        {
            systemInstruction = new
            {
                parts = new object[] { new { text = systemPrompt } }
            },
            contents = new object[]
            {
                new { parts = contentParts.ToArray() }
            },
            generationConfig
        };
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Utf8NoBom, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Payload too large: re-send once with every attachment uploaded to
            // the Files API so the JSON body stays tiny.
            if ((int)response.StatusCode == 413 && !retryWithUpload)
            {
                return await CompleteGeminiWithFilesAsync(
                    attempt, systemPrompt, userPrompt, attachments, jsonSchema, cancellationToken, retryWithUpload: true);
            }

            throw DescribeFailure(attempt.ApiKey, (int)response.StatusCode, raw);
        }

        return ExtractGeminiText(raw);
    }

    private async Task<List<object>> BuildGeminiContentPartsAsync(
        AiAttempt attempt,
        string userPrompt,
        IReadOnlyList<IStudyPlanAiClient.AiAttachment> attachments,
        bool forceFileUpload,
        CancellationToken cancellationToken)
    {
        // Gemini rejects JSON bodies over ~20 MB with HTTP 413. The limit applies
        // to the WHOLE request body, so budget the combined size of all inline
        // attachments (plus generous room for the prompt and JSON overhead).
        var contentParts = new List<object> { new { text = userPrompt } };
        var inlineBudget = TotalInlineBudgetChars - userPrompt.Length - 512 * 1024;
        foreach (var attachment in attachments.Take(5))
        {
            var base64Length = Convert.ToBase64String(attachment.Data).Length;
            if (forceFileUpload || base64Length > MaxInlineBase64Chars || base64Length > inlineBudget)
            {
                var fileUri = await UploadToGeminiFilesAsync(attempt, attachment, cancellationToken);
                contentParts.Add(new { file_data = new { mime_type = attachment.MimeType, file_uri = fileUri } });
            }
            else
            {
                inlineBudget -= base64Length;
                contentParts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = attachment.MimeType,
                        data = Convert.ToBase64String(attachment.Data)
                    }
                });
            }
        }

        return contentParts;
    }

    private async Task<string> CompleteFilesRetryLoopAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<IStudyPlanAiClient.AiAttachment> attachments,
        object? jsonSchema,
        CancellationToken cancellationToken)
    {
        var attempts = BuildAttempts(preferImageChain: true);
        if (attempts.Count == 0)
        {
            throw new HttpRequestException("No AI providers are configured (AiImage / AiContent / Ai sections).");
        }

        attempts = FilterQuotaCooldown(attempts);

        List<Exception>? failures = null;
        for (var i = 0; i < attempts.Count; i++)
        {
            var attempt = attempts[i];
            try
            {
                // Only Gemini supports inline PDF/image parts; text-only providers fall
                // back to the prompt-only path.
                if (attempt.Provider != "gemini")
                {
                    return await CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken, jsonSchema);
                }

                return await CompleteGeminiWithFilesAsync(
                    attempt, systemPrompt, userPrompt, attachments, jsonSchema, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AiQuotaExceededException ex)
            {
                RegisterQuotaCooldown(ex.ApiKey);
                (failures ??= []).Add(new HttpRequestException(
                    $"Provider '{attempt.Provider}' (model '{attempt.Model}') hit its quota; rotating to the next key.", ex));
            }
            catch (Exception ex) when (i < attempts.Count - 1)
            {
                (failures ??= []).Add(new HttpRequestException(
                    $"Provider '{attempt.Provider}' (model '{attempt.Model}') failed: {ex.Message}", ex));
            }
        }

        throw new AggregateException(
            "All AI providers in the fallback chain failed.", failures ?? []);
    }

    private List<AiAttempt> BuildAttempts(bool preferImageChain = false)
    {
        var attempts = new List<AiAttempt>();

        // When files are attached, prefer the multimodal "AiImage" chain (e.g. Gemini)
        // and fall back to the text chains afterwards.
        if (preferImageChain)
        {
            AppendChain(attempts, imageProviders?.Value);
        }

        // Primary chain: the ordered "AiContent" list (fallback strategy).
        AppendChain(attempts, contentProviders?.Value);

        // Legacy single-provider fallback: the "Ai" section, when the chain is empty.
        if (attempts.Count == 0)
        {
            var settings = options.Value;
            var provider = (settings.Provider ?? "gemini").Trim().ToLowerInvariant();
            var apiKey = (settings.ApiKey ?? string.Empty).Trim();
            if (apiKey.Length > 0)
            {
                attempts.Add(new AiAttempt(
                    provider,
                    apiKey,
                    string.IsNullOrWhiteSpace(settings.Model) ? null : settings.Model.Trim(),
                    settings.BaseUrl));
            }
        }

        return attempts;
    }

    private static void AppendChain(List<AiAttempt> attempts, List<AiContentProviderOptions>? chain)
    {
        if (chain is null)
        {
            return;
        }

        foreach (var entry in chain)
        {
            var provider = (entry.Provider ?? string.Empty).Trim().ToLowerInvariant();
            var apiKey = (entry.ApiKey ?? string.Empty).Trim();
            if (provider.Length == 0 || apiKey.Length == 0)
            {
                continue;
            }

            attempts.Add(new AiAttempt(
                provider,
                apiKey,
                entry.Model?.Trim(),
                entry.BaseUrl?.Trim()));
        }
    }

    private async Task<string> CompleteWithAsync(
        AiAttempt attempt,
        string systemPrompt,
        string userPrompt,
        object? jsonSchema,
        CancellationToken cancellationToken)
    {
        if (attempt.Provider == "gemini")
        {
            return await CompleteGeminiAsync(attempt, systemPrompt, userPrompt, jsonSchema, cancellationToken);
        }

        if (attempt.Provider == "pollinations")
        {
            return await CompleteOpenAiAsync(
                "https://text.pollinations.ai/",
                apiKey: null,
                model: "openai",
                systemPrompt,
                userPrompt,
                cancellationToken,
                path: "openai");
        }

        return await CompleteOpenAiAsync(
            NormalizeBaseUrl(attempt.BaseUrl, attempt.Provider),
            attempt.ApiKey,
            ResolveModel(attempt),
            systemPrompt,
            userPrompt,
            cancellationToken);
    }

    private static string ResolveModel(AiAttempt attempt) =>
        string.IsNullOrWhiteSpace(attempt.Model) ? DefaultModel(attempt.Provider) : attempt.Model.Trim();

    private async Task<string> CompleteOpenAiAsync(
        string baseUrl,
        string? apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        string path = "chat/completions")
    {
        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl), path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        var body = apiKey is null
            ? (object)new
            {
                model,
                messages = new object[]
                {
                    new { role = "user", content = $"{systemPrompt}\n\n{userPrompt}" }
                }
            }
            : new
            {
                model,
                temperature = 0.4,
                max_tokens = MaxOutputTokens,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Utf8NoBom, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw DescribeFailure(apiKey ?? string.Empty, (int)response.StatusCode, raw);
        }

        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("finish_reason", out var finishReason)
                && string.Equals(finishReason.GetString(), "length", StringComparison.OrdinalIgnoreCase))
            {
                // The response was cut off mid-JSON; treat it as a failure so the
                // fallback chain can try another provider instead of parsing
                // truncated content.
                throw new HttpRequestException("AI provider response was truncated (max tokens reached).");
            }

            var message = choice.GetProperty("message");
            if (message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

        throw new HttpRequestException("AI provider returned an empty response.");
    }

    private async Task<string> CompleteGeminiAsync(
        AiAttempt attempt,
        string systemPrompt,
        string userPrompt,
        object? jsonSchema,
        CancellationToken cancellationToken)
    {
        var model = NormalizeGeminiModel(attempt.Model);
        var url = BuildGeminiGenerateContentUrl(attempt.BaseUrl, model);
        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("X-goog-api-key", attempt.ApiKey);
        var generationConfig = jsonSchema is null
            ? new Dictionary<string, object?>
            {
                ["responseMimeType"] = "application/json"
            }
            : new Dictionary<string, object?>
            {
                ["responseMimeType"] = "application/json",
                ["responseJsonSchema"] = jsonSchema,
                ["maxOutputTokens"] = MaxOutputTokens
            };

        if (jsonSchema is null)
        {
            generationConfig["maxOutputTokens"] = MaxOutputTokens;
        }
        var body = new
        {
            systemInstruction = new
            {
                parts = new object[] { new { text = systemPrompt } }
            },
            contents = new object[]
            {
                new
                {
                    parts = new object[] { new { text = userPrompt } }
                }
            },
            generationConfig
        };
        var payload = JsonSerializer.Serialize(body, JsonOptions);
        request.Content = new StringContent(payload, Utf8NoBom, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && jsonSchema is not null && (int)response.StatusCode is 400)
        {
            return await CompleteGeminiAsync(attempt, systemPrompt, userPrompt, jsonSchema: null, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw DescribeFailure(attempt.ApiKey, (int)response.StatusCode, raw);
        }

        return ExtractGeminiText(raw);
    }

    private static HttpRequestException DescribeFailure(string apiKey, int statusCode, string raw)
    {
        if (statusCode == 429
            || raw.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("quota", StringComparison.OrdinalIgnoreCase))
        {
            return new AiQuotaExceededException(apiKey, $"AI provider key hit its quota (HTTP {statusCode}).");
        }

        return new HttpRequestException($"AI provider returned {statusCode}.");
    }

    private static string ExtractGeminiText(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        if (TryReadGenerateContentText(root, out var fromCandidates))
        {
            return fromCandidates;
        }
        if (root.TryGetProperty("status", out var statusEl))
        {
            var status = statusEl.GetString();
            if (status is "failed" or "cancelled")
            {
                throw new HttpRequestException($"AI provider returned status {status}.");
            }
        }

        if (TryReadString(root, "output_text", out var outputText))
        {
            return outputText;
        }

        if (TryCollectText(root, "steps", out var fromSteps))
        {
            return fromSteps;
        }

        if (TryCollectText(root, "outputs", out var fromOutputs))
        {
            return fromOutputs;
        }

        throw new HttpRequestException("AI provider returned an empty response.");
    }

    private static bool TryReadGenerateContentText(JsonElement root, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return false;
        }

        var candidate = candidates[0];
        if (candidate.ValueKind != JsonValueKind.Object
            || !candidate.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.Object
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            AppendTextPart(text, part);
        }

        value = text.ToString().Trim();
        return value.Length > 0;
    }

    private static bool TryReadString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = el.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
    }

    private static bool TryCollectText(JsonElement root, string arrayName, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parts = new StringBuilder();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var type = item.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (type is "thought")
            {
                continue;
            }

            if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in content.EnumerateArray())
                {
                    AppendTextPart(parts, part);
                }

                continue;
            }

            AppendTextPart(parts, item);
        }

        value = parts.ToString().Trim();
        return value.Length > 0;
    }

    private static void AppendTextPart(StringBuilder parts, JsonElement part)
    {
        if (part.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var type = part.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
        if (type is not null and not "text")
        {
            return;
        }

        if (part.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
        {
            parts.Append(textEl.GetString());
        }
    }

    private static string DefaultModel(string provider) =>
        provider switch
        {
            "gemini" => "gemini-flash-latest",
            "grok" => "grok-4.6",
            "openai" or "chatgpt" => "gpt-5",
            _ => "llama-3.1-8b-instant"
        };

    private static string NormalizeGeminiModel(string? model)
    {
        var value = string.IsNullOrWhiteSpace(model) ? "gemini-flash-latest" : model.Trim();
        const string suffix = ":generateContent";
        if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^suffix.Length];
        }

        return value;
    }

    private static string BuildGeminiGenerateContentUrl(string? baseUrl, string model)
    {
        var value = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(value)
            || value.EndsWith("/interactions", StringComparison.OrdinalIgnoreCase))
        {
            value = "https://generativelanguage.googleapis.com/v1beta";
        }

        if (value.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
        {
            return $"{value}/{model}:generateContent";
        }

        if (value.Contains("/models/", StringComparison.OrdinalIgnoreCase)
            && value.Contains(":generateContent", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return $"{value}/models/{model}:generateContent";
    }

    private static string NormalizeBaseUrl(string? baseUrl, string provider)
    {
        var value = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)
            || value.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
        {
            value = provider switch
            {
                "openai" or "chatgpt" => "https://api.openai.com/v1/",
                "grok" => "https://api.x.ai/v1/",
                _ => "https://api.groq.com/openai/v1/"
            };
        }

        return value.EndsWith('/') ? value : value + "/";
    }

    private sealed record AiAttempt(string Provider, string ApiKey, string? Model, string? BaseUrl);
}
