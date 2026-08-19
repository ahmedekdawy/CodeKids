using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record SaveWeeklyStudyPlanCommand(
    Guid TeacherId,
    Guid? Id,
    Guid CourseId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Notes,
    IReadOnlyList<SaveWeeklyStudyPlanWeekDto> Weeks) : ICommand<WeeklyStudyPlanDto>;
