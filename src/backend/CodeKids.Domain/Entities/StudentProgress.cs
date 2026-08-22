namespace CodeKids.Domain.Entities;

public class StudentProgress : TenantEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public Guid StepId { get; set; }
    public bool IsCompleted { get; set; }
    public int EarnedXp { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public User? User { get; set; }
}

