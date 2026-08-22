namespace CodeKids.Domain.Entities;

public class ClassroomStudent : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Classroom? Classroom { get; set; }
    public User? Student { get; set; }
}
