using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Notifications;
using CodeKids.Infrastructure.Tenancy;
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

/// <summary>
/// Delivers a notification two ways: over SignalR to pages that are open, and as a web push to the
/// user's subscribed devices so it also arrives when the app is closed.
/// </summary>
public sealed class NotificationRealtime(
    IHubContext<NotificationHub> hub,
    WebPushQueue webPushQueue,
    ITenantContext tenantContext) : INotificationRealtime
{
    public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        webPushQueue.Enqueue(
            tenantContext.TenantId,
            userId,
            notification.Id,
            notification.Title,
            notification.Body,
            notification.TargetUrl);
        return hub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("notification", notification, cancellationToken);
    }
}
