using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class SetManagedUserActiveCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SetManagedUserActiveCommand, ManagedUserDto>
{
    public async Task<ManagedUserDto> Handle(SetManagedUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.UserId && !command.IsActive)
        {
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        var user = await dbContext.Users
            .Include(x => x.CourseRates)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!command.IsActive && user.Role == UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(
                x => x.Role == UserRole.SuperAdmin && x.IsActive,
                cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot deactivate the last Super Admin.");
            }
        }

        user.IsActive = command.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateManagedUserCommandHandler.ToDto(user);
    }
}
