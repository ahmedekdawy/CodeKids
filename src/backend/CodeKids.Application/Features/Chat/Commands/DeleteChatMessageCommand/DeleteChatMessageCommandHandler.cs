using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class DeleteChatMessageCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteChatMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(DeleteChatMessageCommand command, CancellationToken cancellationToken)
    {
        if (!ChatAccess.CanModerate(command.Role))
        {
            throw new InvalidOperationException("Only teachers can delete chat messages.");
        }

        var message = await dbContext.ChatMessages
            .Include(x => x.Sender)
            .FirstOrDefaultAsync(x => x.Id == command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("Chat message not found.");

        await ChatAccess.RequireMemberAsync(dbContext, message.RoomId, command.UserId, cancellationToken);

        message.IsDeleted = true;
        message.DeletedByUserId = command.UserId;
        message.DeletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ChatAccess.ToDto(message, message.Sender?.DisplayName ?? string.Empty);
    }
}
