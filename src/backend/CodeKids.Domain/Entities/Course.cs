using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AgeMin { get; set; } = 8;
    public int AgeMax { get; set; } = 12;
    public CourseTerm Term { get; set; } = CourseTerm.FullYear;
    public int Grade { get; set; } = 1;
    public int SortOrder { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
    public List<Quiz> Quizzes { get; set; } = [];
}
