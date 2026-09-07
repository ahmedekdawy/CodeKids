using CodeKids.Application.Abstractions;
using CodeKids.Application.Common;
using CodeKids.Application.Options;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed class ForgotPasswordCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppMessageSender whatsAppSender,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(2);
    private const int MaxRequestsPerDay = 5;

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var channel = NormalizeChannel(command.Channel);
        var acceptedMessage = channel == "email"
            ? "If an account exists for that login, a reset link has been sent by email."
            : "If an account exists for that login, a reset link has been sent on WhatsApp.";

        var login = (command.EmailOrMobile ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        var user = await FindUserAsync(login, cancellationToken);
        if (user is null || !user.IsActive)
        {
            logger.LogInformation("Password reset requested for unknown or inactive login.");
            return new ForgotPasswordResult(true, acceptedMessage);
        }

        if (channel == "whatsapp")
        {
            if (string.IsNullOrWhiteSpace(user.MobilePhone))
            {
                logger.LogInformation("Password reset WhatsApp requested for phone-less user {UserId}.", user.Id);
                return new ForgotPasswordResult(true, acceptedMessage);
            }

            if (!IsEgyptianMobile(user.MobilePhone))
            {
                throw ApiException.Create(
                    "api.errors.auth.resetUseEmailForNonEgyptian",
                    "WhatsApp reset works with Egyptian mobile numbers only. Please use email instead.");
            }
        }
        else if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
        {
            logger.LogInformation("Password reset email requested for email-less user {UserId}.", user.Id);
            return new ForgotPasswordResult(true, acceptedMessage);
        }
        else
        {
            var mail = emailOptions.Value;
            if (!mail.Enabled || string.IsNullOrWhiteSpace(mail.Host))
            {
                throw ApiException.Create(
                    "api.errors.auth.resetEmailUnavailable",
                    "Email reset is not available right now. Please use WhatsApp if your number is Egyptian.");
            }
        }

        await EnsureRateLimitAsync(user.Id, cancellationToken);

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

        if (channel == "email")
        {
            var subject = "CodeKids password reset";
            var body =
                $"Hello {user.DisplayName},\n\n" +
                "You requested a CodeKids password reset.\n" +
                "Open this link within one hour:\n\n" +
                $"{resetUrl}\n\n" +
                "If you did not request this, ignore this email.";

            try
            {
                await emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Password reset email send failed for user {UserId}.", user.Id);
            }
        }
        else
        {
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
        }

        return new ForgotPasswordResult(true, acceptedMessage);
    }

    private async Task EnsureRateLimitAsync(Guid userId, CancellationToken cancellationToken)
    {
        var dayStart = DateTimeOffset.UtcNow.AddHours(-24);

        var recent = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == userId && x.CreatedAtUtc >= dayStart)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (recent.Count >= MaxRequestsPerDay)
        {
            throw ApiException.Create(
                "api.errors.auth.resetDailyLimit",
                "Password reset limit reached for this account. Try again tomorrow.",
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

    private static string NormalizeChannel(string? channel)
    {
        var value = (channel ?? string.Empty).Trim().ToLowerInvariant();
        return value is "email" or "mail" ? "email" : "whatsapp";
    }

    /// <summary>Egyptian numbers after normalization start with 20 (e.g. 2010… / 01… → 201…).</summary>
    internal static bool IsEgyptianMobile(string? phone)
    {
        var digits = DigitsOnly(phone);
        if (digits.Length == 0)
        {
            return false;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith('0') && digits.Length is 10 or 11)
        {
            digits = "2" + digits;
        }

        return digits.StartsWith("20", StringComparison.Ordinal) && digits.Length is >= 11 and <= 13;
    }

    private static string DigitsOnly(string? value)
    {
        var raw = value ?? string.Empty;
        var builder = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
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
