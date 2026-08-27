namespace CodeKids.Domain.Entities;

public class SubjectUnit : TenantEntity
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public bool StudentAskEnabled { get; set; }
    public Subject? Subject { get; set; }
    public List<SubjectUnitLesson> Lessons { get; set; } = [];
}
