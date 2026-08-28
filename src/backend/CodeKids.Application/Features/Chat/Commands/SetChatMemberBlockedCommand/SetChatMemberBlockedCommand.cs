using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record SetChatMemberBlockedCommand(
    Guid TeacherId,
    string? Role,
    Guid RoomId,
    Guid StudentId,
    bool Blocked) : ICommand<ChatMemberDto>;
