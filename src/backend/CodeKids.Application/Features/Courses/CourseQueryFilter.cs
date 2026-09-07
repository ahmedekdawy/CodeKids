using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

internal static class CourseQueryFilter
{
    public static async Task<IQueryable<Course>> ApplyRoleAsync(
        IAppDbContext dbContext,
        IQueryable<Course> coursesQuery,
        Guid? userId,
        string? role,
        CancellationToken cancellationToken)
    {
        if (!PublishedCourseAccess.CanViewUnpublished(role))
        {
            coursesQuery = coursesQuery.Where(c => c.IsPublished);
        }

        if (queryRoleIs(role, UserRole.Student) && userId is Guid studentId)
        {
            var student = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == studentId)
                .Select(x => new { x.Grade, x.SchoolType })
                .FirstOrDefaultAsync(cancellationToken);

            var visibleCourseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                dbContext, studentId, cancellationToken);
            var studentGrade = student?.Grade;
            var studentSchoolType = student?.SchoolType;

            return coursesQuery.Where(c =>
                visibleCourseIds.Contains(c.Id)
                && (studentGrade == null
                    || c.Grade == studentGrade
                    || (c.Grade == null && c.StageId == null)
                    || (c.Grade == null
                        && c.StageId != null
                        && dbContext.Grades.Any(g => g.Id == studentGrade && g.StageId == c.StageId)))
                && (c.SchoolType == null
                    || c.SchoolType == SchoolType.All
                    || studentSchoolType == null
                    || studentSchoolType == SchoolType.All
                    || c.SchoolType == studentSchoolType));
        }

        if (queryRoleIs(role, UserRole.Teacher) && userId is Guid teacherId)
        {
            var teacherCourseIds = await dbContext.ClassroomCourses
                .AsNoTracking()
                .Where(x => x.TeacherId == teacherId)
                .Select(x => x.CourseId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return coursesQuery.Where(c => teacherCourseIds.Contains(c.Id));
        }

        return coursesQuery;
    }

    private static bool queryRoleIs(string? role, UserRole expected) =>
        string.Equals(role, expected.ToString(), StringComparison.OrdinalIgnoreCase);
}
