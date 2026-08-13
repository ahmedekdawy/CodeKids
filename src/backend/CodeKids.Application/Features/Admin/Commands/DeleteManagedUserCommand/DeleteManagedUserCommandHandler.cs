using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class DeleteManagedUserCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteManagedUserCommand, bool>
{
    public async Task<bool> Handle(DeleteManagedUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.UserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role == UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last Super Admin.");
            }
        }

        var teacherLinks = await dbContext.ClassroomCourses
            .Where(x => x.TeacherId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(teacherLinks);

        var memberships = await dbContext.ClassroomStudents
            .Where(x => x.StudentId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomStudents.RemoveRange(memberships);

        var courseEnrollments = await dbContext.StudentCourseEnrollments
            .Where(x => x.StudentId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.StudentCourseEnrollments.RemoveRange(courseEnrollments);

        var children = await dbContext.Users.Where(x => x.ParentId == user.Id).ToListAsync(cancellationToken);
        foreach (var child in children)
        {
            child.ParentId = null;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
