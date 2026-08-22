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
        var provider = (settings.Provider ?? "groq").Trim().ToLowerInvariant();
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
        var model = string.IsNullOrWhiteSpace(settings.Model) ? "gemini-2.0-flash" : settings.Model.Trim();
        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.ApiKey.Trim())}";
        var client = httpClientFactory.CreateClient(nameof(StudyPlanAiClient));
        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            },
            generationConfig = new
            {
                temperature = 0.4,
                responseMimeType = "application/json"
            }
        };
        using var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI provider returned {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();
        return text ?? string.Empty;
    }

    private static string DefaultModel(string provider) =>
        provider == "gemini" ? "gemini-2.0-flash" : "llama-3.1-8b-instant";

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
