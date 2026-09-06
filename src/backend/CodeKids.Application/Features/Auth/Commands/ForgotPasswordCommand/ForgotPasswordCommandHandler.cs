using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Common;
using CodeKids.Application.Options;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Application.Features.Auth;

public sealed class ForgotPasswordCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppMessageSender whatsAppSender,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(2);
    private const int MaxRequestsPerDay = 5;

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        const string acceptedMessage = "If an account exists for that login, a reset link has been sent on WhatsApp.";
        var login = (command.EmailOrMobile ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        var user = await FindUserAsync(login, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.MobilePhone))
        {
            logger.LogInformation("Password reset requested for unknown or phone-less login.");
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        await EnsureRateLimitAsync(user.MobilePhone, cancellationToken);

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

        var baseUrl = EnsureHttps(frontendOptions.Value.BaseUrl).TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        // Keep the URL alone on its own lines so WhatsApp still auto-links it next to Arabic RTL text.
        var message =
            $"مرحباً {user.DisplayName}\n\n" +
            "طلبت إعادة تعيين كلمة مرور CodeKids.\n" +
            "افتح هذا الرابط خلال ساعة:\n\n" +
            $"{resetUrl}\n\n" +
            "إذا لم تطلب ذلك، تجاهل هذه الرسالة.";

        var send = await whatsAppSender.SendMessageAsync(
            user.MobilePhone,
            message,
            cancellationToken,
            ruleKey: "password_reset",
            username: user.Email);

        if (!send.Success)
        {
            logger.LogWarning(
                "Password reset WhatsApp send failed for user {UserId}: {Error}",
                user.Id,
                send.Error);
        }

        return new ForgotPasswordResult(true, acceptedMessage);
    }

    private async Task EnsureRateLimitAsync(string mobilePhone, CancellationToken cancellationToken)
    {
        var phone = mobilePhone.Trim();
        var dayStart = DateTimeOffset.UtcNow.AddHours(-24);

        var recent = await (
            from token in dbContext.PasswordResetTokens
            join u in dbContext.Users on token.UserId equals u.Id
            where u.MobilePhone == phone && token.CreatedAtUtc >= dayStart
            orderby token.CreatedAtUtc descending
            select token.CreatedAtUtc
        ).ToListAsync(cancellationToken);

        if (recent.Count >= MaxRequestsPerDay)
        {
            throw ApiException.Create(
                "api.errors.auth.resetDailyLimit",
                "Password reset limit reached for this number. Try again tomorrow.",
                ("max", MaxRequestsPerDay.ToString()));
        }

        if (recent.Count > 0)
        {
            var elapsed = DateTimeOffset.UtcNow - recent[0];
            if (elapsed < MinInterval)
            {
                var wait = MinInterval - elapsed;
                var waitMinutes = Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes));
                throw ApiException.Create(
                    "api.errors.auth.resetTooSoon",
                    "Please wait before requesting another password reset.",
                    ("minutes", waitMinutes.ToString()));
            }
        }
    }

    private static string EnsureHttps(string url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed["http://".Length..];
        }

        if (!trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && trimmed.Length > 0)
        {
            return "https://" + trimmed;
        }

        return trimmed;
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
