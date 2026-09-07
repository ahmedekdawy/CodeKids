namespace CodeKids.Application.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@codekids.local";
    public string FromDisplayName { get; set; } = "CodeKids";
}

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";
    public string BaseUrl { get; set; } = "http://localhost:4200";
}

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Send a WhatsApp message with a deep link when an assignment, quiz, or exam is published.</summary>
    public bool WhatsAppOnAssessmentPublish { get; set; } = true;

    /// <summary>Also message the parent of every notified student.</summary>
    public bool NotifyParentsOnAssessmentPublish { get; set; } = true;
}

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>gemini, grok, groq, openai, or pollinations. Gemini uses generateContent; grok/groq/openai use chat completions.</summary>
    public string Provider { get; set; } = "grok";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "grok-4.6";
    public string BaseUrl { get; set; } = "https://api.x.ai/v1/";
}
