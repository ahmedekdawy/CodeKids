using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class SendChatMessageCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SendChatMessageCommand, ChatMessageDto>
{
    private const int MaxLength = 2000;

    public async Task<ChatMessageDto> Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
    {
        var body = (command.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Message is required.");
        }

        if (body.Length > MaxLength)
        {
            throw new InvalidOperationException("Message is too long.");
        }

        var member = await ChatAccess.RequireMemberAsync(dbContext, command.RoomId, command.UserId, cancellationToken);
        await ChatAccess.EnsureCanSendAsync(dbContext, member, command.Role, cancellationToken);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            RoomId = command.RoomId,
            SenderId = command.UserId,
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        var name = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == command.UserId)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return ChatAccess.ToDto(message, name);
    }
}
