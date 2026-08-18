using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.WeeklyReports;

internal static class WeeklyReportAccess
{
    internal static async Task<IReadOnlyList<Guid>> GetTeacherStudentIdsAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        int? grade,
        CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.Classroom!.Courses.Any(t => t.TeacherId == teacherId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
        {
            return [];
        }

        var studentsQuery = dbContext.Users
            .AsNoTracking()
            .Where(x => studentIds.Contains(x.Id) && x.Role == UserRole.Student);

        if (grade.HasValue)
        {
            studentsQuery = studentsQuery.Where(x => x.Grade == grade.Value);
        }

        return await studentsQuery
            .OrderBy(x => x.Grade)
            .ThenBy(x => x.DisplayName)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    internal static async Task EnsureTeacherOwnsStudentsAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return;
        }

        var allowed = await GetTeacherStudentIdsAsync(dbContext, teacherId, grade: null, cancellationToken);
        var allowedSet = allowed.ToHashSet();
        var invalid = studentIds.Where(x => !allowedSet.Contains(x)).ToList();
        if (invalid.Count > 0)
        {
            throw new InvalidOperationException("One or more students are not assigned to this teacher.");
        }
    }
}
