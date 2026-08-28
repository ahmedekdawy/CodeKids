using System.Security.Claims;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Chat;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api.Hubs;

[Authorize(Roles = "Student,Teacher,SuperAdmin")]
public sealed class ChatHub(
    IAppDbContext dbContext,
    ICommandHandler<SendChatMessageCommand, ChatMessageDto> sendHandler) : Hub
{
    public static string GroupName(Guid roomId) => $"chat-{roomId:D}";

    public static string UserGroup(Guid userId) => $"chat-user-{userId:D}";

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUser.GetUserId(Context.User!);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = CurrentUser.GetUserId(Context.User!);
        await ChatAccess.RequireMemberAsync(dbContext, roomId, userId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    public async Task SendMessage(Guid roomId, string body)
    {
        var userId = CurrentUser.GetUserId(Context.User!);
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value
            ?? Context.User?.FindFirst("role")?.Value;
        var dto = await sendHandler.Handle(
            new SendChatMessageCommand(userId, role, roomId, body),
            Context.ConnectionAborted);
        await PublishMessageAsync(name => Clients.Group(name), dbContext, dto, Context.ConnectionAborted);
    }

    public static Task PublishMessageAsync(
        IHubContext<ChatHub> hub,
        IAppDbContext db,
        ChatMessageDto dto,
        CancellationToken cancellationToken) =>
        PublishMessageAsync(name => hub.Clients.Group(name), db, dto, cancellationToken);

    public static Task PublishDeletedAsync(
        IHubContext<ChatHub> hub,
        IAppDbContext db,
        ChatMessageDto dto,
        CancellationToken cancellationToken) =>
        PublishDeletedAsync(name => hub.Clients.Group(name), db, dto, cancellationToken);

    public static Task PublishMemberAsync(
        IHubContext<ChatHub> hub,
        IAppDbContext db,
        Guid roomId,
        ChatMemberDto dto,
        CancellationToken cancellationToken) =>
        PublishMemberAsync(name => hub.Clients.Group(name), db, roomId, dto, cancellationToken);

    public static Task PublishMessageAsync(
        Func<string, IClientProxy> group,
        IAppDbContext db,
        ChatMessageDto dto,
        CancellationToken cancellationToken) =>
        PublishToMembersAsync(group, db, dto.RoomId, "message", dto, cancellationToken);

    public static Task PublishDeletedAsync(
        Func<string, IClientProxy> group,
        IAppDbContext db,
        ChatMessageDto dto,
        CancellationToken cancellationToken) =>
        PublishToMembersAsync(group, db, dto.RoomId, "messageDeleted", dto, cancellationToken);

    public static Task PublishMemberAsync(
        Func<string, IClientProxy> group,
        IAppDbContext db,
        Guid roomId,
        ChatMemberDto dto,
        CancellationToken cancellationToken) =>
        PublishToMembersAsync(group, db, roomId, "memberUpdated", dto, cancellationToken);

    private static async Task PublishToMembersAsync(
        Func<string, IClientProxy> group,
        IAppDbContext db,
        Guid roomId,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        await group(GroupName(roomId)).SendAsync(eventName, payload, cancellationToken);
        var memberIds = await db.ChatRoomMembers
            .AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
        foreach (var memberId in memberIds.Distinct())
        {
            await group(UserGroup(memberId)).SendAsync(eventName, payload, cancellationToken);
        }
    }
}
