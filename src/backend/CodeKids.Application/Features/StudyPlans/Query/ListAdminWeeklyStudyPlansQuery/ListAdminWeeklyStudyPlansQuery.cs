using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record ListAdminWeeklyStudyPlansQuery(
    Guid? TeacherId,
    Guid? CourseId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string SortKey,
    string SortDir,
    int Page,
    int PageSize) : IQuery<PagedWeeklyStudyPlansResultDto>;
