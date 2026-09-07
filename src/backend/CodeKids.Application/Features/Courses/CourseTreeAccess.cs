using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public static class CourseTreeAccess
{
    public const string TeacherOnlyMessage = "You can only edit the course tree for courses assigned to you.";
    public const int PromptMax = 2000;

    public static async Task EnsureCanManageCourseAsync(
        IAppDbContext dbContext,
        Guid? userId,
        string? role,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase) || userId is null)
        {
            throw new InvalidOperationException(TeacherOnlyMessage);
        }

        var assigned = await dbContext.ClassroomCourses
            .AsNoTracking()
            .AnyAsync(x => x.TeacherId == userId.Value && x.CourseId == courseId, cancellationToken);

        if (!assigned)
        {
            throw new InvalidOperationException(TeacherOnlyMessage);
        }
    }
}
