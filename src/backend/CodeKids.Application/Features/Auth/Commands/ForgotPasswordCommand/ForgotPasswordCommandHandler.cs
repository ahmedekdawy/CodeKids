using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Application.Features.Auth;

public sealed class ForgotPasswordCommandHandler(
    IAppDbContext dbContext,
    IEmailService emailSender,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        const string acceptedMessage = "If an account exists for that login, a reset link has been sent.";
        var login = (command.EmailOrMobile ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        var user = await FindUserAsync(login, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
        {
            logger.LogInformation("Password reset requested for unknown or email-less login.");
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        var existing = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in existing)
        {
            item.UsedAtUtc = DateTimeOffset.UtcNow;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.PasswordResetTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var body =
            $"Hi {user.DisplayName},{Environment.NewLine}{Environment.NewLine}" +
            $"We received a request to reset your CodeKids password.{Environment.NewLine}" +
            $"Open this link within 1 hour:{Environment.NewLine}{resetUrl}{Environment.NewLine}{Environment.NewLine}" +
            "If you did not request this, you can ignore this email.";

        await emailSender.SendEmailAsync(user.Email, "Reset your CodeKids password", body);
        return new ForgotPasswordResult(true, acceptedMessage);
    }

    private async Task<User?> FindUserAsync(string login, CancellationToken cancellationToken)
    {
        if (login.Contains('@'))
        {
            var email = login.ToLowerInvariant();
            return await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }

        var phone = RegisterCommandHandler.NormalizePhone(login);
        if (string.IsNullOrWhiteSpace(phone)) return null;
        return await dbContext.Users.FirstOrDefaultAsync(x => x.MobilePhone == phone, cancellationToken);
    }

    internal static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
