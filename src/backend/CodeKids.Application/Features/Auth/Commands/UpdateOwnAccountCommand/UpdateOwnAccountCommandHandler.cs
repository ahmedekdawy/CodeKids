using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Auth;

public sealed class UpdateOwnAccountCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<UpdateOwnAccountCommand, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(UpdateOwnAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

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

        return RegisterCommandHandler.ToDto(user);
    }
}
