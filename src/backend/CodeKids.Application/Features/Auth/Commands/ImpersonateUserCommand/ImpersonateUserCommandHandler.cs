using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Auth;

public sealed class ImpersonateUserCommandHandler(
    IAppDbContext dbContext,
    IJwtTokenService jwtTokenService) : ICommandHandler<ImpersonateUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(ImpersonateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.TargetUserId)
        {
            throw new InvalidOperationException("You cannot impersonate your own account.");
        }

        var actor = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.AdminUserId, cancellationToken)
            ?? throw new InvalidOperationException("User account not found.");

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.TargetUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (actor.Role == UserRole.SuperAdmin)
        {
            if (user.Role is not (UserRole.Student or UserRole.Parent or UserRole.Teacher))
            {
                throw new InvalidOperationException("Only teacher, parent, or student accounts can be used.");
            }
        }
        else if (actor.Role == UserRole.Teacher)
        {
            if (user.Role != UserRole.Student)
            {
                throw new InvalidOperationException("Teachers can only sign in as their enrolled students.");
            }

            var enrolled = await dbContext.ClassroomStudents
                .AsNoTracking()
                .Include(x => x.Classroom)
                .AnyAsync(
                    x => x.StudentId == command.TargetUserId
                         && x.Classroom!.Courses.Any(t => t.TeacherId == command.AdminUserId),
                    cancellationToken);

            if (!enrolled)
            {
                throw new InvalidOperationException("Student is not in your classrooms.");
            }
        }
        else
        {
            throw new InvalidOperationException("Only Super Admin can impersonate users.");
        }

        return new AuthResponse(jwtTokenService.CreateToken(user), RegisterCommandHandler.ToDto(user));
    }
}
