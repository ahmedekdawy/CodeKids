using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed record DeleteStudentClassroomAttendanceCommand(
    Guid AttendanceId,
    Guid? TeacherId,
    bool IsAdmin) : ICommand<bool>;
