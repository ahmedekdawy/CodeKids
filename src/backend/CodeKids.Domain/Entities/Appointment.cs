namespace CodeKids.Domain.Entities;

public class Appointment : TenantEntity
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid CourseId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
}
