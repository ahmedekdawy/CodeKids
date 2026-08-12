using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

internal static class TeacherSessionAttendanceValidators
{
    public static async Task ValidateAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        DateOnly sessionDate,
        CancellationToken cancellationToken)
    {
        if (sessionDate == default)
        {
            throw new InvalidOperationException("Session date is required.");
        }

        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher not found.");

        if (teacher.Role != UserRole.Teacher)
        {
            throw new InvalidOperationException("Selected user must be a teacher.");
        }

        _ = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var duplicate = await dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .AnyAsync(
                x => x.TeacherId == teacherId
                     && x.CourseId == courseId
                     && x.SessionDate == sessionDate,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Attendance for this teacher, course, and date already exists.");
        }
    }

    public static async Task<TeacherSessionAttendanceDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return ToDto(row);
    }

    public static TeacherSessionAttendanceDto ToDto(TeacherSessionAttendance row)
    {
        var teacherName = row.Teacher?.DisplayName ?? string.Empty;
        var courseName = row.Course?.Title ?? string.Empty;
        var courseGrade = row.Course?.Grade;
        var gradeLabel = courseGrade switch
        {
            null => "All",
            -1 => "KG1",
            0 => "KG2",
            _ => $"Grade {courseGrade}"
        };
        var label = string.Join(
            " - ",
            new[] { gradeLabel, courseName, teacherName, row.SessionDate.ToString("yyyy-MM-dd") }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        return new TeacherSessionAttendanceDto(
            row.Id,
            row.TeacherId,
            teacherName,
            row.CourseId,
            courseName,
            courseGrade,
            row.SessionDate,
            label);
    }
}
