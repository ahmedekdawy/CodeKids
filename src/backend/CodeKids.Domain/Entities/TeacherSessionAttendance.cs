namespace CodeKids.Domain.Entities;

/// <summary>Records that a teacher delivered a session for a course on a given date.</summary>
public class TeacherSessionAttendance : TenantEntity
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid CourseId { get; set; }
    /// <summary>Calendar date of the session (local school day).</summary>
    public DateOnly SessionDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
}
