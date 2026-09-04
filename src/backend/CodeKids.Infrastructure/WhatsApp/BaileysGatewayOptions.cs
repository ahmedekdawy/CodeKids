namespace CodeKids.Infrastructure.WhatsApp;

public sealed class BaileysGatewayOptions
{
    public const string SectionName = "WhatsApp:Baileys";

    /// <summary>Base URL of the Node/Baileys service, e.g. http://localhost:3000/.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Optional shared secret sent as the <c>X-Api-Key</c> header.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int SendTimeoutSeconds { get; set; } = 90;

    /// <summary>Minimum idle time before a session may be reused.</summary>
    public int CooldownMinMilliseconds { get; set; } = 5000;

    public int CooldownMaxMilliseconds { get; set; } = 10000;

    /// <summary>Send a "composing" presence and pause before the message.</summary>
    public bool SimulateTyping { get; set; } = true;

    public int TypingMinMilliseconds { get; set; } = 2000;

    public int TypingMaxMilliseconds { get; set; } = 4000;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
