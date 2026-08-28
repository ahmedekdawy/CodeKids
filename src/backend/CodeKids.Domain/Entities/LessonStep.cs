namespace CodeKids.Domain.Entities;

public class LessonStep : TenantEntity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public int StepNumber { get; set; }
}

