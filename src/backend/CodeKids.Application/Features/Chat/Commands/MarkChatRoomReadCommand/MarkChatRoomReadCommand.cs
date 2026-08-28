using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record MarkChatRoomReadCommand(Guid UserId, Guid RoomId) : ICommand<int>;
