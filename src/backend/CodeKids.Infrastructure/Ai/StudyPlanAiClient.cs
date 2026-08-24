using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Ai;

public sealed class StudyPlanAiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options) : IStudyPlanAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var provider = (settings.Provider ?? "gemini").Trim().ToLowerInvariant();
        var apiKey = (settings.ApiKey ?? string.Empty).Trim();

        if (provider is "gemini" && apiKey.Length > 0)
        {
            return await CompleteGeminiAsync(settings, systemPrompt, userPrompt, cancellationToken);
        }

        if (apiKey.Length > 0 && provider is not "pollinations")
        {
            return await CompleteOpenAiAsync(
                NormalizeBaseUrl(settings.BaseUrl, provider),
                apiKey,
                string.IsNullOrWhiteSpace(settings.Model) ? DefaultModel(provider) : settings.Model.Trim(),
                systemPrompt,
                userPrompt,
                cancellationToken);
        }

        return await CompleteOpenAiAsync(
            "https://text.pollinations.ai/",
            apiKey: null,
            model: string.IsNullOrWhiteSpace(settings.Model) || provider is "groq" or "openai"
                ? "openai"
                : settings.Model.Trim(),
            systemPrompt,
            userPrompt,
            cancellationToken,
            path: "openai");
    }

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
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

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
            var message = choices[0].GetProperty("message");
            if (message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

        throw new HttpRequestException("AI provider returned an empty response.");
    }

    private async Task<string> CompleteGeminiAsync(
        AiOptions settings,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(settings.Model) ? "gemini-2.5-flash" : settings.Model.Trim();
        var url = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta/interactions"
            : settings.BaseUrl.Trim().TrimEnd('/');
        if (!url.EndsWith("/interactions", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://generativelanguage.googleapis.com/v1beta/interactions";
        }

        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", settings.ApiKey.Trim());
        request.Headers.TryAddWithoutValidation("Api-Revision", "2026-05-20");
        var body = new
        {
            model,
            input = userPrompt,
            system_instruction = systemPrompt,
            store = false,
            response_format = new
            {
                type = "text",
                mime_type = "application/json",
                schema = StudyPlanResponseSchema
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI provider returned {(int)response.StatusCode}.");
        }

        return ExtractGeminiText(raw);
    }

    private static readonly object StudyPlanResponseSchema = new
    {
        type = "object",
        properties = new
        {
            notes = new { type = "string" },
            weeks = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        weekNumber = new { type = "integer" },
                        topics = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    highlight = new { type = "boolean" }
                                },
                                required = new[] { "title", "highlight" }
                            }
                        }
                    },
                    required = new[] { "weekNumber", "topics" }
                }
            }
        },
        required = new[] { "notes", "weeks" }
    };

    private static string ExtractGeminiText(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
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
        provider == "gemini" ? "gemini-2.5-flash" : "llama-3.1-8b-instant";

    private static string NormalizeBaseUrl(string? baseUrl, string provider)
    {
        var value = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = provider == "openai"
                ? "https://api.openai.com/v1/"
                : "https://api.groq.com/openai/v1/";
        }

        return value.EndsWith('/') ? value : value + "/";
    }
}
