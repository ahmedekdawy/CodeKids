namespace CodeKids.Application.Features.Courses;

public sealed record CourseLessonDto(
    Guid Id,
    Guid? UnitId,
    string Title,
    string Theme,
    string Description,
    int Difficulty,
    int XpReward,
    int SortOrder,
    int StepCount);
