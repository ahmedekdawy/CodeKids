using CodeKids.Application.Features.Notifications;

namespace CodeKids.Application.Abstractions;

public interface INotificationRealtime
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
