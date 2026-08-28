namespace CodeKids.Domain.Entities;

public class StudentAskedQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LessonId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string AiAnswer { get; set; } = string.Empty;
    public bool AiInScope { get; set; }
    public string TeacherAnswer { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TeacherAnsweredAtUtc { get; set; }

    public User? Student { get; set; }
    public Course? Course { get; set; }
    public User? Teacher { get; set; }
}
