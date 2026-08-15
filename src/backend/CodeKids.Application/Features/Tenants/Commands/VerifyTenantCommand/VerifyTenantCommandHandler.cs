using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Tenants;

public sealed class VerifyTenantCommandHandler(
    IAppDbContext dbContext) : ICommandHandler<VerifyTenantCommand, VerifyTenantResult>
{
    public async Task<VerifyTenantResult> Handle(VerifyTenantCommand command, CancellationToken cancellationToken)
    {
        var raw = (command.Token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Verification token is invalid or has expired.");
        }

        var hash = ForgotPasswordCommandHandler.HashToken(raw);
        var signup = await dbContext.TenantSignups.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            ?? throw new InvalidOperationException("Verification token is invalid or has expired.");

        if (signup.VerifiedAtUtc is not null)
        {
            return new VerifyTenantResult(signup.TenantSlug, signup.Email, "Tenant already verified. You can sign in.");
        }

        if (signup.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Verification token is invalid or has expired.");
        }

        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == signup.Email, cancellationToken))
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        signup.VerifiedAtUtc = DateTimeOffset.UtcNow;
        signup.TenantId = signup.TenantSlug;

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = signup.Email,
            DisplayName = signup.DisplayName,
            PasswordHash = signup.PasswordHash,
            Role = UserRole.SuperAdmin,
            TenantId = signup.TenantSlug,
            TotalXp = 0
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new VerifyTenantResult(signup.TenantSlug, signup.Email, "Email verified. Your tenant is ready. Sign in to continue.");
    }
}
