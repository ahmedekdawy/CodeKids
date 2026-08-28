using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class SetChatMemberBlockedCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SetChatMemberBlockedCommand, ChatMemberDto>
{
    public async Task<ChatMemberDto> Handle(SetChatMemberBlockedCommand command, CancellationToken cancellationToken)
    {
        if (!ChatAccess.CanModerate(command.Role))
        {
            throw new InvalidOperationException("Only teachers can block students from chat.");
        }

        await ChatAccess.RequireMemberAsync(dbContext, command.RoomId, command.TeacherId, cancellationToken);

        var member = await dbContext.ChatRoomMembers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.RoomId == command.RoomId && x.UserId == command.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student is not in this chat.");

        if (member.User?.Role != UserRole.Student)
        {
            throw new InvalidOperationException("You can only block students.");
        }

        member.IsBlocked = command.Blocked;
        member.BlockedAtUtc = command.Blocked ? DateTimeOffset.UtcNow : null;
        member.BlockedByUserId = command.Blocked ? command.TeacherId : null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChatMemberDto(
            member.UserId,
            member.User?.DisplayName ?? string.Empty,
            nameof(UserRole.Student),
            member.IsBlocked);
    }
}
