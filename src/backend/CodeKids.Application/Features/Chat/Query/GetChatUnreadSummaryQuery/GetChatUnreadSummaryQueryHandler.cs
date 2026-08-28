using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class GetChatUnreadSummaryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetChatUnreadSummaryQuery, ChatUnreadSummaryDto>
{
    public async Task<ChatUnreadSummaryDto> Handle(
        GetChatUnreadSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var roomIds = await dbContext.ChatRoomMembers
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => x.RoomId)
            .ToListAsync(cancellationToken);

        if (roomIds.Count == 0)
        {
            return new ChatUnreadSummaryDto(0, null, string.Empty);
        }

        var unreadCounts = await ChatAccess.UnreadCountsAsync(
            dbContext, query.UserId, roomIds, cancellationToken);
        var total = unreadCounts.Values.Sum();
        if (total == 0)
        {
            return new ChatUnreadSummaryDto(0, null, string.Empty);
        }

        var latest = await (
            from m in dbContext.ChatMessages.AsNoTracking()
            join mem in dbContext.ChatRoomMembers.AsNoTracking()
                on new { m.RoomId, UserId = query.UserId } equals new { mem.RoomId, mem.UserId }
            where roomIds.Contains(m.RoomId)
                  && !m.IsDeleted
                  && m.SenderId != query.UserId
                  && (mem.LastReadAtUtc == null || m.CreatedAtUtc > mem.LastReadAtUtc)
            orderby m.CreatedAtUtc descending
            select new { m.RoomId }
        ).FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
        {
            return new ChatUnreadSummaryDto(total, null, string.Empty);
        }

        var title = await dbContext.ChatRooms
            .AsNoTracking()
            .Where(x => x.Id == latest.RoomId)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new ChatUnreadSummaryDto(total, latest.RoomId, title);
    }
}
