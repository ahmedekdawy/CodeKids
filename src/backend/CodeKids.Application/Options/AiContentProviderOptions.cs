namespace CodeKids.Application.Options;

/// <summary>
/// One entry in the "AiContent" fallback chain. Providers are tried in order until one succeeds.
/// gemini uses generateContent; grok/groq/openai/chatgpt use OpenAI-style chat completions.
/// </summary>
public sealed class AiContentProviderOptions
{
    public string Provider { get; set; } = "grok";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
