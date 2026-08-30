using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Notifications;

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand<int>;

public sealed class MarkAllNotificationsReadCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(
        MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        var unread = await dbContext.UserNotifications
            .Where(x => x.UserId == command.UserId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var row in unread)
        {
            row.IsRead = true;
        }

        if (unread.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return unread.Count;
    }
}
