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

public sealed class ResetPasswordCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher) : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var rawToken = (command.Token ?? string.Empty).Trim();
        var newPassword = command.NewPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new InvalidOperationException("Reset token is invalid or has expired.");
        }

        if (newPassword.Trim().Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters.");
        }

        var hash = ForgotPasswordCommandHandler.HashToken(rawToken);
        var token = await dbContext.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (token is null || token.UsedAtUtc is not null || token.ExpiresAtUtc < DateTimeOffset.UtcNow || token.User is null)
        {
            throw new InvalidOperationException("Reset token is invalid or has expired.");
        }

        token.User.PasswordHash = passwordHasher.Hash(newPassword.Trim());
        token.UsedAtUtc = DateTimeOffset.UtcNow;

        var siblings = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == token.UserId && x.Id != token.Id && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblings)
        {
            sibling.UsedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
