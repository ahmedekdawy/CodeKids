namespace CodeKids.Domain.Entities;

/// <summary>Teacher-authored weekly evaluation for a student.</summary>
public class StudentWeeklyReport
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    /// <summary>Monday of the evaluated week (local school calendar).</summary>
    public DateOnly WeekStartDate { get; set; }
    public int? PerformancePercent { get; set; }
    public int? AttendancePercent { get; set; }
    public int? HomeworkPercent { get; set; }
    public string InteractionDuringSession { get; set; } = string.Empty;
    public bool? OpenCamera { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public User? Student { get; set; }
}
