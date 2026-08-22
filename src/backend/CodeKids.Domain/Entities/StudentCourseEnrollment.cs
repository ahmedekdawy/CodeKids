namespace CodeKids.Domain.Entities;

public class StudentCourseEnrollment : TenantEntity
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid CourseId { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Student { get; set; }
    public Classroom? Classroom { get; set; }
    public Course? Course { get; set; }
}
