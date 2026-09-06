namespace CodeKids.Infrastructure.WhatsApp;

public sealed class WhatsProOptions
{
    public const string SectionName = "WhatsApp:WhatsPro";

    /// <summary>API root that the routes below are appended to.</summary>
    public string BaseUrl { get; set; } = "https://whats-pro.net/backend/public/index.php/api/";

    /// <summary>Passphrase the gateway uses to decrypt the payload.</summary>
    public string EncryptionKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string LoginRoute { get; set; } = "user/login";

    public string SendRoute { get; set; } = "user/messages/send";

    /// <summary>Field carrying the destination number in the send payload.</summary>
    public string PhoneField { get; set; } = "phone";

    /// <summary>Field carrying the message body in the send payload.</summary>
    public string MessageField { get; set; } = "message";

    public int SendTimeoutSeconds { get; set; } = 90;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(EncryptionKey)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password);
}
