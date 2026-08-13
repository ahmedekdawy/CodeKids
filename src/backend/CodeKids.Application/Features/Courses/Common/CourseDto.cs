namespace CodeKids.Application.Features.Courses;

public sealed record CourseDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string? Term,
    int? Grade,
    string? SchoolType,
    int SortOrder,
    IReadOnlyList<CourseUnitDto> Units,
    IReadOnlyList<CourseLessonDto> Lessons,
    IReadOnlyList<CourseQuizDto> Quizzes);
