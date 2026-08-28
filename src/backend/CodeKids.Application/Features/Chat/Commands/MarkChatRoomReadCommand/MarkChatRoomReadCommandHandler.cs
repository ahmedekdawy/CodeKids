using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed class MarkChatRoomReadCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<MarkChatRoomReadCommand, int>
{
    public async Task<int> Handle(MarkChatRoomReadCommand command, CancellationToken cancellationToken)
    {
        var member = await ChatAccess.RequireMemberAsync(
            dbContext, command.RoomId, command.UserId, cancellationToken);
        member.LastReadAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return 0;
    }
}
