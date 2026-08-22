using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Application.Options;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeKids.Application.Features.Tenants;

public sealed class RegisterTenantCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEmailService emailSender,
    IOptions<FrontendOptions> frontendOptions) : ICommandHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private const string AcceptedMessage = "Check your email to verify the address and create the tenant.";

    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand command, CancellationToken cancellationToken)
    {
        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var tenantName = (command.TenantName ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();
        var password = command.Password ?? string.Empty;
        var mobile = RegisterCommandHandler.NormalizePhone(command.MobilePhone);

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new InvalidOperationException("Tenant name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new InvalidOperationException("A valid tenant email is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        if (password.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters.");
        }

        var emailTaken = await dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken)
            || await dbContext.TenantSignups.IgnoreQueryFilters()
                .AnyAsync(x => x.Email == email && x.VerifiedAtUtc != null, cancellationToken);
        if (emailTaken)
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(mobile)
            && (await dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.MobilePhone == mobile, cancellationToken)
                || await dbContext.TenantSignups.IgnoreQueryFilters()
                    .AnyAsync(x => x.MobilePhone == mobile && x.VerifiedAtUtc != null, cancellationToken)))
        {
            throw new InvalidOperationException("An account with that mobile number already exists.");
        }

        var slug = await UniqueSlugAsync(tenantName, cancellationToken);
        var pending = await dbContext.TenantSignups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == email && x.VerifiedAtUtc == null, cancellationToken);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        if (pending is null)
        {
            pending = new TenantSignup
            {
                Id = Guid.NewGuid(),
                TenantName = tenantName,
                TenantSlug = slug,
                Email = email,
                MobilePhone = mobile,
                DisplayName = displayName,
                PasswordHash = passwordHasher.Hash(password),
                TokenHash = ForgotPasswordCommandHandler.HashToken(rawToken),
                ExpiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                TenantId = null
            };
            dbContext.TenantSignups.Add(pending);
        }
        else
        {
            pending.TenantName = tenantName;
            pending.TenantSlug = slug;
            pending.DisplayName = displayName;
            pending.MobilePhone = mobile;
            pending.PasswordHash = passwordHasher.Hash(password);
            pending.TokenHash = ForgotPasswordCommandHandler.HashToken(rawToken);
            pending.ExpiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime);
            pending.TenantId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var verifyUrl = $"{baseUrl}/verify-tenant?token={Uri.EscapeDataString(rawToken)}";
        var body =
            $"Hi {displayName},{Environment.NewLine}{Environment.NewLine}" +
            $"Verify this email to create the tenant \"{tenantName}\".{Environment.NewLine}" +
            $"Open this link within 24 hours:{Environment.NewLine}{verifyUrl}{Environment.NewLine}{Environment.NewLine}" +
            "If you did not request this, you can ignore this email.";

        await emailSender.SendEmailAsync(email, "Verify your CodeKids tenant email", body);
        return new RegisterTenantResult(true, AcceptedMessage);
    }

    private async Task<string> UniqueSlugAsync(string tenantName, CancellationToken cancellationToken)
    {
        var root = Regex.Replace(tenantName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(root))
        {
            root = "tenant";
        }

        if (root.Length > 40)
        {
            root = root[..40].Trim('-');
        }

        var slug = root;
        var n = 2;
        while (await dbContext.TenantSignups.IgnoreQueryFilters().AnyAsync(x => x.TenantSlug == slug, cancellationToken)
            || await dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.TenantId == slug, cancellationToken))
        {
            slug = $"{root}-{n}";
            n++;
        }

        return slug;
    }
}
