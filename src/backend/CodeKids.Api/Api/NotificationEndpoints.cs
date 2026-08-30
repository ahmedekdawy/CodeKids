using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class NotificationEndpoints
{
    private static readonly AuthorizeAttribute NotifyRoles =
        new() { Roles = "Student,Parent,Teacher,SuperAdmin" };

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (
            HttpContext httpContext,
            IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>> handler,
            int? limit,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new ListNotificationsQuery(userId, limit ?? 30), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(NotifyRoles);

        app.MapGet("/api/notifications/unread", async (
            HttpContext httpContext,
            IQueryHandler<GetNotificationUnreadSummaryQuery, NotificationUnreadSummaryDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new GetNotificationUnreadSummaryQuery(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(NotifyRoles);

        app.MapPost("/api/notifications/{id:guid}/read", async (
            Guid id,
            HttpContext httpContext,
            ICommandHandler<MarkNotificationReadCommand, NotificationDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new MarkNotificationReadCommand(userId, id), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(NotifyRoles);

        app.MapPost("/api/notifications/read-all", async (
            HttpContext httpContext,
            ICommandHandler<MarkAllNotificationsReadCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new MarkAllNotificationsReadCommand(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(NotifyRoles);

        return app;
    }
}
