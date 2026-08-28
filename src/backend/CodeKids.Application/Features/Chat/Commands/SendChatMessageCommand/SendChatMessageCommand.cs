using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record SendChatMessageCommand(Guid UserId, string? Role, Guid RoomId, string Body)
    : ICommand<ChatMessageDto>;
