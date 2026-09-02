namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomEnrollmentListItemDto(
    Guid ClassroomId,
    string ClassroomName,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    IReadOnlyList<Guid> EnrolledCourseIds,
    IReadOnlyList<string> EnrolledCourseTitles);

public sealed record PagedClassroomEnrollmentsResultDto(
    IReadOnlyList<ClassroomEnrollmentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
