using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record GenerateWeeklyStudyPlanRequest(
    Guid CourseId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Language);

public sealed record GenerateWeeklyStudyPlanResult(
    string Notes,
    IReadOnlyList<SaveWeeklyStudyPlanWeekDto> Weeks);

public sealed record GenerateWeeklyStudyPlanCommand(
    Guid TeacherId,
    Guid CourseId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Language) : ICommand<GenerateWeeklyStudyPlanResult>;
