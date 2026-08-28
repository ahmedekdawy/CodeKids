using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Chat;

public sealed record GetChatUnreadSummaryQuery(Guid UserId) : IQuery<ChatUnreadSummaryDto>;
