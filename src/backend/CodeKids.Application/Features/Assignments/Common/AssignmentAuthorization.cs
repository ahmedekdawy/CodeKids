using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

internal static class AssignmentAuthorization
{
    public static async Task EnsureCanManageClassroomAsync(
        IAppDbContext dbContext,
        Guid teacherUserId,
        Guid classroomId,
        string action,
        CancellationToken cancellationToken)
    {
        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == teacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (isAdmin)
        {
            return;
        }

        var allowed = await dbContext.ClassroomCourses
            .AsNoTracking()
            .AnyAsync(x => x.ClassroomId == classroomId && x.TeacherId == teacherUserId, cancellationToken);
        if (allowed)
        {
            return;
        }

        throw new InvalidOperationException(action switch
        {
            "edit" => "Only an assigned classroom teacher can edit assignments.",
            "delete" => "Only an assigned classroom teacher can delete assignments.",
            _ => "Only an assigned classroom teacher can create assignments."
        });
    }
}
