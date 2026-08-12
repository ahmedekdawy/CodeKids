using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record UpdateCourseLessonRequest(
    Guid? UnitId,
    string Title,
    string Theme,
    string? Description = null,
    int Difficulty = 1,
    int XpReward = 10,
    int SortOrder = 1);

public sealed record UpdateCourseLessonCommand(
    Guid LessonId,
    Guid? UnitId,
    string Title,
    string Theme,
    string? Description = null,
    int Difficulty = 1,
    int XpReward = 10,
    int SortOrder = 1) : ICommand<CourseLessonDto>;
