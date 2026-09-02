using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ListClassroomEnrollmentsQuery(
    Guid? ViewerUserId,
    string? ViewerRole,
    Guid? ClassroomId,
    Guid? CourseId,
    string? StudentSearch,
    string SortKey,
    string SortDir,
    int Page,
    int PageSize) : IQuery<PagedClassroomEnrollmentsResultDto>;
