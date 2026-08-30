using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Notifications;

public sealed record GetNotificationUnreadSummaryQuery(Guid UserId) : IQuery<NotificationUnreadSummaryDto>;

public sealed class GetNotificationUnreadSummaryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetNotificationUnreadSummaryQuery, NotificationUnreadSummaryDto>
{
    public async Task<NotificationUnreadSummaryDto> Handle(
        GetNotificationUnreadSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var count = await dbContext.UserNotifications
            .AsNoTracking()
            .CountAsync(x => x.UserId == query.UserId && !x.IsRead, cancellationToken);
        return new NotificationUnreadSummaryDto(count);
    }
}
