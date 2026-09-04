using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class SendAdminWhatsAppCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppMessageSender sender,
    IWhatsAppClient whatsAppClient) : ICommandHandler<SendAdminWhatsAppCommand, SendAdminWhatsAppResultDto>
{
    public async Task<SendAdminWhatsAppResultDto> Handle(
        SendAdminWhatsAppCommand command,
        CancellationToken cancellationToken)
    {
        var message = command.Message?.Trim() ?? string.Empty;
        if (message.Length == 0)
        {
            throw new InvalidOperationException("Message is required.");
        }

        var phones = (command.Phones ?? [])
            .Select(p => p?.Trim() ?? string.Empty)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (phones.Count == 0)
        {
            throw new InvalidOperationException("At least one phone number is required.");
        }

        var admin = await dbContext.Users
            .Where(x => x.Id == command.AdminUserId)
            .Select(x => new { x.Email, x.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        var username = admin?.Email ?? admin?.DisplayName ?? "admin";
        var shareUrl = whatsAppClient.BuildShareUrl(message);

        var recipients = new List<AdminWhatsAppRecipientDto>(phones.Count);
        var sent = 0;
        var failed = 0;

        foreach (var phone in phones)
        {
            var result = await sender.SendMessageAsync(
                phone,
                message,
                cancellationToken,
                ruleKey: "admin_manual",
                username: username);

            if (result.Success) sent++;
            else failed++;

            recipients.Add(new AdminWhatsAppRecipientDto(
                phone,
                result.Success,
                result.Success ? $"Sent via session {result.SessionId}." : result.Error ?? "Send failed."));
        }

        return new SendAdminWhatsAppResultDto(sent, failed, recipients, shareUrl);
    }
}
