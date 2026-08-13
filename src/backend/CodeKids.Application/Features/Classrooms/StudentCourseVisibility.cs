using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public static class StudentCourseVisibility
{
    /// <summary>
    /// Per classroom: specific course enrollments if any, otherwise all classroom courses.
    /// </summary>
    public static async Task<HashSet<Guid>> GetVisibleCourseIdsAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var classroomIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .Select(x => x.ClassroomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (classroomIds.Count == 0)
        {
            return [];
        }

        var specific = await dbContext.StudentCourseEnrollments
            .AsNoTracking()
            .Where(x => x.StudentId == studentId && classroomIds.Contains(x.ClassroomId))
            .Select(x => new { x.ClassroomId, x.CourseId })
            .ToListAsync(cancellationToken);

        var specificByClassroom = specific
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CourseId).ToHashSet());

        var assigned = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.ClassroomId))
            .Select(x => new { x.ClassroomId, x.CourseId })
            .ToListAsync(cancellationToken);

        var legacy = await dbContext.Classrooms
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.Id) && x.CourseId != null)
            .Select(x => new { ClassroomId = x.Id, CourseId = x.CourseId!.Value })
            .ToListAsync(cancellationToken);

        var visible = new HashSet<Guid>();
        foreach (var classroomId in classroomIds)
        {
            if (specificByClassroom.TryGetValue(classroomId, out var enrolled) && enrolled.Count > 0)
            {
                foreach (var courseId in enrolled)
                {
                    visible.Add(courseId);
                }

                continue;
            }

            foreach (var row in assigned.Where(x => x.ClassroomId == classroomId))
            {
                visible.Add(row.CourseId);
            }

            foreach (var row in legacy.Where(x => x.ClassroomId == classroomId))
            {
                visible.Add(row.CourseId);
            }
        }

        return visible;
    }

    public static HashSet<Guid> EnrolledCourseIdsForClassroom(
        IEnumerable<StudentCourseEnrollment> enrollments,
        Guid studentId,
        Guid classroomId) =>
        enrollments
            .Where(x => x.StudentId == studentId && x.ClassroomId == classroomId)
            .Select(x => x.CourseId)
            .ToHashSet();
}
