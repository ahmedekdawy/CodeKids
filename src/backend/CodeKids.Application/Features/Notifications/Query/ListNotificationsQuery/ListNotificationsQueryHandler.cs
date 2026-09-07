using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Notifications;

public sealed record ListNotificationsQuery(Guid UserId, int Limit = 30) : IQuery<IReadOnlyList<NotificationDto>>;

public sealed class ListNotificationsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(
        ListNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);
        var rows = await dbContext.UserNotifications
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(NotificationPublisher.Map).ToList();
    }
}
