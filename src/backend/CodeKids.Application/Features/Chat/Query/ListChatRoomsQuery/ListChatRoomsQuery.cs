using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record ListChatRoomsQuery(Guid UserId) : IQuery<IReadOnlyList<ChatRoomDto>>;
