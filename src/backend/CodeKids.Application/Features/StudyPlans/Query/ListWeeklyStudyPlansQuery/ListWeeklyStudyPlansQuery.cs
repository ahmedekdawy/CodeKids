using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record ListWeeklyStudyPlansQuery(
    Guid ViewerUserId,
    string ViewerRole,
    Guid? TeacherId = null,
    Guid? CourseId = null,
    Guid? StudentId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<IReadOnlyList<WeeklyStudyPlanDto>>;
