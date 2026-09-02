using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed record ListStudentClassroomAttendanceQuery(
    Guid ViewerUserId,
    string ViewerRole,
    Guid? ClassroomId,
    int? GradeId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? StudentSearch,
    string SortKey,
    string SortDir,
    int Page,
    int PageSize) : IQuery<PagedStudentClassroomAttendanceResultDto>;
