using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class Course : TenantEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AgeMin { get; set; } = 8;
    public int AgeMax { get; set; } = 12;
    public CourseTerm? Term { get; set; }
    /// <summary>Specific grade id (KG1=-1, KG2=0, grades 1–12). Null with StageId = all grades in that stage; both null = all grades.</summary>
    public int? Grade { get; set; }
    /// <summary>Optional stage. When set and Grade is null, the course covers every grade in the stage.</summary>
    public int? StageId { get; set; }
    /// <summary>Arabic, Language, or All; null treated as All.</summary>
    public SchoolType? SchoolType { get; set; }
    public int SortOrder { get; set; }
    public Stage? Stage { get; set; }
    public List<CourseUnit> Units { get; set; } = [];
    public List<Lesson> Lessons { get; set; } = [];
    public List<Quiz> Quizzes { get; set; } = [];
}
