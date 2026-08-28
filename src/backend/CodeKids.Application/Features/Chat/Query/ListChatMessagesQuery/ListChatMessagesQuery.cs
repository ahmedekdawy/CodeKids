using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record ListChatMessagesQuery(Guid UserId, Guid RoomId) : IQuery<IReadOnlyList<ChatMessageDto>>;
