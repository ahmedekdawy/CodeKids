namespace CodeKids.Domain.Entities;

public class CourseUnit : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Course? Course { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
}
