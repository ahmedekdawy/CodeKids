using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed class LoginCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var login = (command.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new InvalidOperationException("Invalid email, mobile, or password.");
        }

        User? user;
        if (login.Contains('@', StringComparison.Ordinal))
        {
            var email = login.ToLowerInvariant();
            user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }
        else
        {
            var phone = RegisterCommandHandler.NormalizePhone(login);
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new InvalidOperationException("Invalid email, mobile, or password.");
            }

            user = await dbContext.Users.FirstOrDefaultAsync(x => x.MobilePhone == phone, cancellationToken);
        }

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email, mobile, or password.");
        }

        return new AuthResponse(
            jwtTokenService.CreateToken(user),
            RegisterCommandHandler.ToDto(user));
    }
}
