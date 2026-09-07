namespace CodeKids.Application.Features.StudyPlans;

public sealed record PagedWeeklyStudyPlansResultDto(
    IReadOnlyList<WeeklyStudyPlanDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
