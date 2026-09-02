namespace CodeKids.Application.Features.Admin;

public sealed record PagedCoursesResultDto(
    IReadOnlyList<CourseSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
