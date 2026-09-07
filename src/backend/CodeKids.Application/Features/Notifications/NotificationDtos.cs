namespace CodeKids.Application.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Kind,
    string Title,
    string Body,
    string TargetUrl,
    Guid? EntityId,
    Guid? RelatedStudentId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record NotificationUnreadSummaryDto(int UnreadCount);
