using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Notifications;

public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : ICommand<NotificationDto>;

public sealed class MarkNotificationReadCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<MarkNotificationReadCommand, NotificationDto>
{
    public async Task<NotificationDto> Handle(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications
            .FirstOrDefaultAsync(x => x.Id == command.NotificationId && x.UserId == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return NotificationPublisher.Map(notification);
    }
}
