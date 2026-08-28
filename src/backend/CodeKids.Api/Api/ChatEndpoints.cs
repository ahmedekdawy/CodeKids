using System.Security.Claims;
using CodeKids.Api.Hubs;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Chat;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeKids.Api;

public static class ChatEndpoints
{
    private static readonly AuthorizeAttribute ChatRoles =
        new() { Roles = "Student,Teacher,SuperAdmin" };

    private static readonly AuthorizeAttribute TeacherOrAdmin =
        new() { Roles = "Teacher,SuperAdmin" };

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/rooms", async (
            HttpContext httpContext,
            IQueryHandler<ListChatRoomsQuery, IReadOnlyList<ChatRoomDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new ListChatRoomsQuery(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(ChatRoles);

        app.MapPost("/api/chat/rooms", async (
            CreateChatRoomRequest request,
            HttpContext httpContext,
            ICommandHandler<CreateChatRoomCommand, ChatRoomDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (!Enum.TryParse<ChatKind>(request.Kind, ignoreCase: true, out var kind))
                {
                    throw new InvalidOperationException("Chat kind must be Direct, Group, or Class.");
                }

                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new CreateChatRoomCommand(
                        teacherId,
                        request.ClassroomId,
                        request.CourseId,
                        request.UnitId,
                        request.LessonId,
                        kind,
                        request.StudentIds ?? []),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapGet("/api/chat/rooms/{id:guid}/messages", async (
            Guid id,
            HttpContext httpContext,
            IQueryHandler<ListChatMessagesQuery, IReadOnlyList<ChatMessageDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new ListChatMessagesQuery(userId, id), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(ChatRoles);

        app.MapPost("/api/chat/rooms/{id:guid}/messages", async (
            Guid id,
            SendChatMessageRequest request,
            HttpContext httpContext,
            IHubContext<ChatHub> hub,
            IAppDbContext dbContext,
            ICommandHandler<SendChatMessageCommand, ChatMessageDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                var dto = await handler.Handle(
                    new SendChatMessageCommand(userId, role, id, request.Body),
                    cancellationToken);
                await ChatHub.PublishMessageAsync(hub, dbContext, dto, cancellationToken);
                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(ChatRoles);

        app.MapDelete("/api/chat/messages/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IHubContext<ChatHub> hub,
            IAppDbContext dbContext,
            ICommandHandler<DeleteChatMessageCommand, ChatMessageDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                var dto = await handler.Handle(
                    new DeleteChatMessageCommand(userId, role, id),
                    cancellationToken);
                await ChatHub.PublishDeletedAsync(hub, dbContext, dto, cancellationToken);
                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapPut("/api/chat/rooms/{roomId:guid}/members/{studentId:guid}/block", async (
            Guid roomId,
            Guid studentId,
            SetChatMemberBlockedRequest request,
            HttpContext httpContext,
            IHubContext<ChatHub> hub,
            IAppDbContext dbContext,
            ICommandHandler<SetChatMemberBlockedCommand, ChatMemberDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                var dto = await handler.Handle(
                    new SetChatMemberBlockedCommand(teacherId, role, roomId, studentId, request.Blocked),
                    cancellationToken);
                await ChatHub.PublishMemberAsync(hub, dbContext, roomId, dto, cancellationToken);
                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapGet("/api/chat/unread", async (
            HttpContext httpContext,
            IQueryHandler<GetChatUnreadSummaryQuery, ChatUnreadSummaryDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new GetChatUnreadSummaryQuery(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(ChatRoles);

        app.MapPut("/api/chat/rooms/{id:guid}/read", async (
            Guid id,
            HttpContext httpContext,
            ICommandHandler<MarkChatRoomReadCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new MarkChatRoomReadCommand(userId, id), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(ChatRoles);

        return app;
    }
}
