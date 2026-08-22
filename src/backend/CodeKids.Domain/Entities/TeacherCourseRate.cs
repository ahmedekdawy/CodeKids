namespace CodeKids.Domain.Entities;

/// <summary>Per-subject pay override for a teacher (session amount and/or monthly salary).</summary>
public class TeacherCourseRate : TenantEntity
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid CourseId { get; set; }
    /// <summary>Special amount per session for this subject (session contracts).</summary>
    public decimal? SessionAmount { get; set; }
    /// <summary>Custom monthly salary for this subject (monthly contracts).</summary>
    public decimal? MonthlySalary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
}
