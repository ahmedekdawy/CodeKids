using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class ListChatMessagesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListChatMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public async Task<IReadOnlyList<ChatMessageDto>> Handle(ListChatMessagesQuery query, CancellationToken cancellationToken)
    {
        await ChatAccess.RequireMemberAsync(dbContext, query.RoomId, query.UserId, cancellationToken);
        var rows = await dbContext.ChatMessages
            .AsNoTracking()
            .Include(x => x.Sender)
            .Where(x => x.RoomId == query.RoomId)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        return rows.Select(x => ChatAccess.ToDto(x, x.Sender?.DisplayName ?? string.Empty)).ToList();
    }
}
