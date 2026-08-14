using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

internal static class FixedTimetableValidators
{
    public static async Task ValidateAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        int dayOfWeek,
        int sessionNumber,
        TimetablePeriod period,
        Guid? excludeEntryId,
        CancellationToken cancellationToken)
    {
        if (dayOfWeek is < 0 or > 6)
        {
            throw new InvalidOperationException("Day of week must be between 0 (Sunday) and 6 (Saturday).");
        }

        if (sessionNumber is < 1 or > 6)
        {
            throw new InvalidOperationException("Session number must be between 1 and 6.");
        }

        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher not found.");

        if (teacher.Role != UserRole.Teacher)
        {
            throw new InvalidOperationException("Selected user must be a teacher.");
        }

        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var slotEntries = await dbContext.FixedTimetableEntries
            .AsNoTracking()
            .Include(x => x.Course)
            .Where(x =>
                x.DayOfWeek == dayOfWeek
                && x.SessionNumber == sessionNumber
                && x.Period == period
                && (excludeEntryId == null || x.Id != excludeEntryId))
            .ToListAsync(cancellationToken);

        if (slotEntries.Any(x => x.TeacherId == teacherId))
        {
            throw new InvalidOperationException("Teacher already has a timetable session in this slot.");
        }

        if (slotEntries.Any(x => course.Grade==x.Course?.Grade&& SchoolTypesConflict(course.SchoolType, x.Course?.SchoolType)))
        {
            throw new InvalidOperationException(
                "A course with the same school type (or All) is already in this timetable slot.");
        }
    }

    internal static bool SchoolTypesConflict(SchoolType? left, SchoolType? right)
    {
        var a = left ?? SchoolType.All;
        var b = right ?? SchoolType.All;
        if (a == SchoolType.All || b == SchoolType.All)
        {
            return true;
        }

        return a == b;
    }

    public static async Task<FixedTimetableEntryDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.FixedTimetableEntries
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return ToDto(entry);
    }

    public static FixedTimetableEntryDto ToDto(FixedTimetableEntry entry)
    {
        var teacherName = entry.Teacher?.DisplayName ?? string.Empty;
        var courseName = entry.Course?.Title ?? string.Empty;
        var courseGrade = entry.Course?.Grade;
        var gradeLabel = courseGrade switch
        {
            null => "All",
            -1 => "KG1",
            0 => "KG2",
            _ => $"Grade {courseGrade}"
        };
        var label = string.Join(
            " - ",
            new[] { gradeLabel, courseName, teacherName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new FixedTimetableEntryDto(
            entry.Id,
            entry.TeacherId,
            teacherName,
            entry.CourseId,
            courseName,
            courseGrade,
            entry.DayOfWeek,
            entry.SessionNumber,
            entry.Period == TimetablePeriod.Pm ? "pm" : "am",
            label);
    }
}
