namespace CodeKids.Domain.Entities;

public class ClassroomCourse
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid CourseId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Classroom? Classroom { get; set; }
    public Course? Course { get; set; }
    public User? Teacher { get; set; }
}
