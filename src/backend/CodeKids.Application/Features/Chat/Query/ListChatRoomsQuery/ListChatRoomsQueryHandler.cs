using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class ListChatRoomsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListChatRoomsQuery, IReadOnlyList<ChatRoomDto>>
{
    public async Task<IReadOnlyList<ChatRoomDto>> Handle(ListChatRoomsQuery query, CancellationToken cancellationToken)
    {
        var roomIds = await dbContext.ChatRoomMembers
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => x.RoomId)
            .ToListAsync(cancellationToken);

        var rooms = await dbContext.ChatRooms
            .AsNoTracking()
            .Include(x => x.Classroom)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Where(x => roomIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var userIds = rooms.SelectMany(r => r.Members.Select(m => m.UserId)).Distinct().ToList();
        var roles = await dbContext.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Role, cancellationToken);

        var unreadCounts = await ChatAccess.UnreadCountsAsync(
            dbContext, query.UserId, roomIds, cancellationToken);

        return rooms.Select(r => ChatAccess.ToDto(
            r,
            query.UserId,
            roles,
            unreadCounts.GetValueOrDefault(r.Id))).ToList();
    }
}
