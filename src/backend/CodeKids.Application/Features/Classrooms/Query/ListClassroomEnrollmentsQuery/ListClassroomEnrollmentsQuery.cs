using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ListClassroomEnrollmentsQuery(
    Guid? ClassroomId,
    Guid? CourseId,
    string? StudentSearch,
    string SortKey,
    string SortDir,
    int Page,
    int PageSize) : IQuery<PagedClassroomEnrollmentsResultDto>;
