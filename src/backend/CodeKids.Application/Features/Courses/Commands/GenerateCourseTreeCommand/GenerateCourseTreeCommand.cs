using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record GenerateCourseTreeRequest(
    string Mode,
    string? Prompt,
    string? Language,
    bool Apply);

public sealed record GeneratedCourseTreeLessonDto(
    string Title,
    int SortOrder);

public sealed record GeneratedCourseTreeUnitDto(
    string Title,
    int SortOrder,
    IReadOnlyList<GeneratedCourseTreeLessonDto> Lessons);

public sealed record GenerateCourseTreeResult(
    string Notes,
    string Mode,
    bool Applied,
    IReadOnlyList<GeneratedCourseTreeUnitDto> Units);

public sealed record GenerateCourseTreeCommand(
    Guid CourseId,
    string Mode,
    string? Prompt,
    string? Language,
    bool Apply) : ICommand<GenerateCourseTreeResult>;
