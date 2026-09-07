using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed record CreateStudentClassroomAttendanceCommand(
    Guid RecordedByTeacherId,
    Guid StudentId,
    Guid ClassroomId,
    DateOnly AttendanceDate,
    string? Status,
    bool IsAdmin) : ICommand<StudentClassroomAttendanceDto>;
