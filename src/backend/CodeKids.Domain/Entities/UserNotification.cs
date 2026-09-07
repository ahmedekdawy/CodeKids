using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class UserNotification : TenantEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? RelatedStudentId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
