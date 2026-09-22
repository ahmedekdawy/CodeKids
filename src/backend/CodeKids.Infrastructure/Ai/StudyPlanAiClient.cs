using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Ai;

public sealed class StudyPlanAiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    IOptions<List<AiContentProviderOptions>>? contentProviders) : IStudyPlanAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private const int MaxOutputTokens = 16384;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

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

    private List<AiAttempt> BuildAttempts()
    {
        var attempts = new List<AiAttempt>();

        // Primary chain: the ordered "AiContent" list (fallback strategy).
        var chain = contentProviders?.Value;
        if (chain is not null)
        {
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
            throw new HttpRequestException($"AI provider returned {(int)response.StatusCode}.");
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
            throw new HttpRequestException($"AI provider returned {(int)response.StatusCode}.");
        }

        return ExtractGeminiText(raw);
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
