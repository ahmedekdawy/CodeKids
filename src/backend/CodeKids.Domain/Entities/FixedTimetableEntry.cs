using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class FixedTimetableEntry : TenantEntity
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid CourseId { get; set; }
    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }
    /// <summary>Session number within the period (1–N, configured per AM/PM).</summary>
    public int SessionNumber { get; set; }
    public TimetablePeriod Period { get; set; }
    /// <summary>Optional comma-separated grade codes this session is shared across. Empty = use the course audience.</summary>
    public string? CombinedGrades { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
}
