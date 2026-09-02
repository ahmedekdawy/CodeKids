namespace CodeKids.Application.Features.StudentAttendance;

public sealed record StudentClassroomAttendanceDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    int? StudentGradeId,
    Guid ClassroomId,
    string ClassroomName,
    DateOnly AttendanceDate,
    string Status,
    Guid RecordedByTeacherId,
    string RecordedByTeacherName,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedStudentClassroomAttendanceResultDto(
    IReadOnlyList<StudentClassroomAttendanceDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CreateStudentClassroomAttendanceRequest(
    Guid StudentId,
    Guid ClassroomId,
    DateOnly AttendanceDate,
    string? Status);

public sealed record CreateMyStudentClassroomAttendanceRequest(
    Guid StudentId,
    Guid ClassroomId,
    DateOnly AttendanceDate,
    string? Status);
