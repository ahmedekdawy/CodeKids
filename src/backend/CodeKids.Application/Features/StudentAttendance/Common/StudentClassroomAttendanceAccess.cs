using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAttendance;

internal static class StudentClassroomAttendanceAccess
{
    internal static StudentAttendanceStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return StudentAttendanceStatus.Present;
        }

        return Enum.TryParse<StudentAttendanceStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Attendance status must be Present or Absent.");
    }

    internal static void ValidateDateRange(DateOnly? fromDate, DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }
    }

    internal static async Task EnsureTeacherOwnsClassroomAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var owns = await dbContext.ClassroomCourses
            .AsNoTracking()
            .AnyAsync(x => x.ClassroomId == classroomId && x.TeacherId == teacherId, cancellationToken);

        if (!owns)
        {
            throw new InvalidOperationException("This classroom is not assigned to you.");
        }
    }

    internal static async Task EnsureStudentInClassroomAsync(
        IAppDbContext dbContext,
        Guid studentId,
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var enrolled = await dbContext.ClassroomStudents
            .AsNoTracking()
            .AnyAsync(x => x.ClassroomId == classroomId && x.StudentId == studentId, cancellationToken);

        if (!enrolled)
        {
            throw new InvalidOperationException("Student is not enrolled in this classroom.");
        }
    }

    internal static async Task<StudentClassroomAttendanceDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.StudentClassroomAttendances
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Classroom)
            .Include(x => x.RecordedByTeacher)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return ToDto(row);
    }

    internal static StudentClassroomAttendanceDto ToDto(Domain.Entities.StudentClassroomAttendance row) =>
        new(
            row.Id,
            row.StudentId,
            row.Student?.DisplayName ?? string.Empty,
            row.Student?.Email ?? string.Empty,
            row.Student?.Grade,
            row.ClassroomId,
            row.Classroom?.Name ?? string.Empty,
            row.AttendanceDate,
            row.Status.ToString(),
            row.RecordedByTeacherId,
            row.RecordedByTeacher?.DisplayName ?? string.Empty,
            row.CreatedAtUtc);
}
