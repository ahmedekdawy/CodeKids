namespace CodeKids.Domain.Entities;

public class CourseUnit : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    /// <summary>1 = first term, 2 = second term. Null when the unit is not term-specific.</summary>
    public int? Term { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public Course? Course { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
}
