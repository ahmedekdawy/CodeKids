namespace CodeKids.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public int XpReward { get; set; }
    public int SortOrder { get; set; }
    public Course? Course { get; set; }
    public List<LessonStep> Steps { get; set; } = [];
    public List<LessonVideo> Videos { get; set; } = [];
}

