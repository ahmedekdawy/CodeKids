namespace CodeKids.Application.Features.Courses;

public sealed record CourseUnitDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<CourseLessonDto> Lessons);
