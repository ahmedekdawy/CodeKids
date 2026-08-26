namespace CodeKids.Domain.Entities;

public class SubjectUnitLesson : TenantEntity
{
    public int Id { get; set; }
    public int SubjectUnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public SubjectUnit? Unit { get; set; }
}
