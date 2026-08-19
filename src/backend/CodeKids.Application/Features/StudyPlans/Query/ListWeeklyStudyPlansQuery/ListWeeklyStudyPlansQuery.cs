using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record ListWeeklyStudyPlansQuery(
    Guid TeacherId,
    Guid? CourseId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<IReadOnlyList<WeeklyStudyPlanDto>>;
