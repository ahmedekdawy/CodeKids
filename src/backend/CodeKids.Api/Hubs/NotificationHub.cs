using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeKids.Api.Hubs;

[Authorize(Roles = "Student,Parent,Teacher,SuperAdmin")]
public sealed class NotificationHub : Hub
{
    public static string UserGroup(Guid userId) => $"notify-user-{userId:D}";

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUser.GetUserId(Context.User!);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }
}

public sealed class NotificationRealtime(IHubContext<NotificationHub> hub) : INotificationRealtime
{
    public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("notification", notification, cancellationToken);
}
