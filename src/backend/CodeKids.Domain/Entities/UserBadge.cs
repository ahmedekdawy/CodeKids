namespace CodeKids.Domain.Entities;

public class UserBadge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTimeOffset AwardedAtUtc { get; set; }
    public User? User { get; set; }
    public Badge? Badge { get; set; }
}
