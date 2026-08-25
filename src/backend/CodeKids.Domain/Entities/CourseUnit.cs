using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class CourseUnit : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    /// <summary>FirstTerm = 1, SecondTerm = 2, FullYear = 3. Null when the unit is not term-specific.</summary>
    public CourseTerm? TermId { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public Course? Course { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
}
