namespace CodeKids.Application.Abstractions;

public interface IWhatsAppMessageSender
{
    Task<WhatsAppMessageResult> SendMessageAsync(
        string phone,
        string message,
        CancellationToken cancellationToken,
        string? ruleKey = null,
        string username = "system");

    Task<WhatsAppMessageResult> SendNotificationAsync(
        string phone,
        string template,
        IReadOnlyDictionary<string, string?> variables,
        CancellationToken cancellationToken,
        string? ruleKey = null,
        string username = "system");

    Task<bool> IsNumberOnWhatsAppAsync(string phone, CancellationToken cancellationToken);
}

public sealed record WhatsAppMessageResult(bool Success, string? SessionId, string? Error)
{
    public static WhatsAppMessageResult Ok(string sessionId) => new(true, sessionId, null);

    public static WhatsAppMessageResult Fail(string error) => new(false, null, error);
}
