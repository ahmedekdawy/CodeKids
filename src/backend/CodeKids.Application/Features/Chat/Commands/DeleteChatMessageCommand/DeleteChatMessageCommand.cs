using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record DeleteChatMessageCommand(Guid UserId, string? Role, Guid MessageId) : ICommand<ChatMessageDto>;
