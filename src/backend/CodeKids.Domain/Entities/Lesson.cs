namespace CodeKids.Domain.Entities;

public class Lesson : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public int XpReward { get; set; }
    public int SortOrder { get; set; }
    /// <summary>When true, students may use Ask on this lesson (or if the unit/course enables Ask).</summary>
    public bool StudentAskEnabled { get; set; }
    public Course? Course { get; set; }
    public CourseUnit? Unit { get; set; }
    public List<LessonStep> Steps { get; set; } = [];
    public List<LessonVideo> Videos { get; set; } = [];
}

