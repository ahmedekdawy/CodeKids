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

public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public bool Enabled { get; set; } = true;

    /// <summary>Local hour (server time) when the daily dump starts.</summary>
    public int HourLocal { get; set; } = 2;

    /// <summary>Run only while minute is less than this window (same pattern as daily reports).</summary>
    public int MinuteWindow { get; set; } = 10;

    /// <summary>Local folder for dump files and the last-run stamp.</summary>
    public string LocalRootPath { get; set; } = "App_Data/backups";

    /// <summary>Upload each dump through the configured media storage (Terabox or Local).</summary>
    public bool UploadToMediaStorage { get; set; } = true;

    /// <summary>Delete local dump files older than this many days. 0 keeps forever.</summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>Path to pg_dump (or just "pg_dump" when it is on PATH).</summary>
    public string PgDumpPath { get; set; } = "pg_dump";

    /// <summary>Keep the local dump after a successful upload.</summary>
    public bool KeepLocalAfterUpload { get; set; } = true;
}
