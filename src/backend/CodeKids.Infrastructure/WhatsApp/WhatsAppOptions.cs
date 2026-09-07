namespace CodeKids.Infrastructure.WhatsApp;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>
    /// <c>WhatsPro</c> / <c>Baileys</c> use the configured gateway sender.
    /// <c>Cloud</c> / <c>Meta</c> use the Meta WhatsApp Cloud API (AccessToken + PhoneNumberId).
    /// </summary>
    public string Provider { get; set; } = "WhatsPro";

    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v21.0";

    public bool UseCloudApi =>
        string.Equals(Provider, "Cloud", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Provider, "Meta", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(PhoneNumberId);
}
