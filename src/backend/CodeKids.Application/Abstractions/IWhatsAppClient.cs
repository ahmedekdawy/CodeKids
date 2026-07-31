namespace CodeKids.Application.Abstractions;

public interface IWhatsAppClient
{
    Task<WhatsAppSendResult> SendTextAsync(string phoneE164, string message, CancellationToken cancellationToken);

    string BuildShareUrl(string message);
}

public sealed record WhatsAppSendResult(bool Sent, string Detail);
