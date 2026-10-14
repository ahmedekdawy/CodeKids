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
            user = await FindInTenantAsync(dbContext.Users.IgnoreQueryFilters().Where(x => x.Email == email), cancellationToken);
        }
        else
        {
            var phone = RegisterCommandHandler.NormalizePhone(login);
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new InvalidOperationException("Invalid email, mobile, or password.");
            }

            user = await FindInTenantAsync(dbContext.Users.IgnoreQueryFilters().Where(x => x.MobilePhone == phone), cancellationToken);
        }

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email, mobile, or password.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("This account is inactive.");
        }

        user.LastLoginDateUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            jwtTokenService.CreateToken(user),
            RegisterCommandHandler.ToDto(user));
    }

    /// <summary>
    /// The same email or mobile can exist in several tenants, so the account in the request's tenant wins.
    /// With no match there, an account from another tenant is used only when it is the single match,
    /// so someone who arrives without their school code can still sign in.
    /// </summary>
    private async Task<User?> FindInTenantAsync(IQueryable<User> matches, CancellationToken cancellationToken)
    {
        var tenantId = dbContext.CurrentTenantId;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var inTenant = await matches.FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
            if (inTenant is not null)
            {
                return inTenant;
            }
        }

        var candidates = await matches.Take(2).ToListAsync(cancellationToken);
        return candidates.Count == 1 ? candidates[0] : null;
    }
}
