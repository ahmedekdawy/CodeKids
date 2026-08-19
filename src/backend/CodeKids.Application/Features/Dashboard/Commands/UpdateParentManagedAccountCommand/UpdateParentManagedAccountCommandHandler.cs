using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed class UpdateParentManagedAccountCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<UpdateParentManagedAccountCommand, ParentManagedAccountDto>
{
    public async Task<ParentManagedAccountDto> Handle(
        UpdateParentManagedAccountCommand command,
        CancellationToken cancellationToken)
    {
        var parent = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == command.ParentId && x.Role == UserRole.Parent, cancellationToken)
            ?? throw new InvalidOperationException("Parent account not found.");

        var user = command.TargetUserId == parent.Id
            ? parent
            : await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.TargetUserId, cancellationToken)
                ?? throw new InvalidOperationException("Student not found.");

        if (user.Id != parent.Id
            && (user.Role != UserRole.Student || user.ParentId != parent.Id))
        {
            throw new InvalidOperationException("This student is not linked to your account.");
        }

        var email = CreateManagedUserCommandHandler.NormalizeEmail(command.Email);
        var mobile = RegisterCommandHandler.NormalizePhone(command.MobilePhone);
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
        {
            throw new InvalidOperationException("Email or mobile is required.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await dbContext.Users.AnyAsync(x => x.Email == email && x.Id != user.Id, cancellationToken))
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(mobile)
            && await dbContext.Users.AnyAsync(x => x.MobilePhone == mobile && x.Id != user.Id, cancellationToken))
        {
            throw new InvalidOperationException("An account with that mobile number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            if (command.Password.Trim().Length < 6)
            {
                throw new InvalidOperationException("Password must be at least 6 characters.");
            }

            user.PasswordHash = passwordHasher.Hash(command.Password.Trim());
        }

        user.Email = email;
        user.MobilePhone = mobile;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ParentManagedAccountDto(
            user.Id,
            user.DisplayName,
            user.Role.ToString(),
            user.Email,
            user.MobilePhone);
    }
}
