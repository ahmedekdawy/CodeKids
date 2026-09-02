using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Admin;

public sealed record ListAdminCoursesQuery(
    string? TitleSearch,
    int? StageId,
    int? Grade,
    string SortKey,
    string SortDir,
    int Page,
    int PageSize) : IQuery<PagedCoursesResultDto>;
