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

        var admin = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.AdminUserId, cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");
        if (admin.Role != UserRole.SuperAdmin)
        {
            throw new InvalidOperationException("Only Super Admin can impersonate users.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.TargetUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role is not (UserRole.Student or UserRole.Parent or UserRole.Teacher))
        {
            throw new InvalidOperationException("Only teacher, parent, or student accounts can be used.");
        }

        return new AuthResponse(jwtTokenService.CreateToken(user), RegisterCommandHandler.ToDto(user));
    }
}
