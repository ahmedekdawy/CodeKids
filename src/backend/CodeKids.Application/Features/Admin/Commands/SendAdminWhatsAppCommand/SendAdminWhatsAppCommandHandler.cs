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

        var admin = await dbContext.Users
            .Where(x => x.Id == command.AdminUserId)
            .Select(x => new { x.Email, x.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        var username = admin?.Email ?? admin?.DisplayName ?? "admin";
        var shareUrl = whatsAppClient.BuildShareUrl(message);

        if (command.SendToGroup)
        {
            var groupId = command.GroupId?.Trim() ?? string.Empty;
            if (groupId.Length == 0)
            {
                throw new InvalidOperationException("Group id is required when sending to a WhatsApp group.");
            }

            var groupResult = await sender.SendGroupMessageAsync(
                groupId,
                message,
                cancellationToken,
                ruleKey: "admin_group",
                username: username);

            return new SendAdminWhatsAppResultDto(
                groupResult.Success ? 1 : 0,
                groupResult.Success ? 0 : 1,
                [
                    new AdminWhatsAppRecipientDto(
                        groupId,
                        groupResult.Success,
                        groupResult.Success
                            ? $"Sent to group via {groupResult.SessionId}."
                            : groupResult.Error ?? "Send failed.")
                ],
                shareUrl);
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
