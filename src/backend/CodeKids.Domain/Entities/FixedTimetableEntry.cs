using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class FixedTimetableEntry
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid CourseId { get; set; }
    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }
    /// <summary>Session number within the period (1–6).</summary>
    public int SessionNumber { get; set; }
    public TimetablePeriod Period { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
}
