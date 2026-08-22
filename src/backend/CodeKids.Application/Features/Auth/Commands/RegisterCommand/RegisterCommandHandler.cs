using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed class RegisterCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role) ||
            role is not (UserRole.Student or UserRole.Parent))
        {
            throw new InvalidOperationException("Public registration is limited to Student or Parent. Teachers are created by Super Admin.");
        }

        if (role == UserRole.Student && command.ParentId is Guid parentId)
        {
            var parent = await dbContext.Users.FirstOrDefaultAsync(
                x => x.Id == parentId && x.Role == UserRole.Parent,
                cancellationToken);
            if (parent is null)
            {
                throw new InvalidOperationException("Parent account was not found.");
            }
        }

        var defaultAvatar = await dbContext.Avatars
            .OrderBy(x => x.UnlockXp)
            .FirstOrDefaultAsync(cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = command.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(command.Password),
            Role = role,
            ParentId = role == UserRole.Student ? command.ParentId : null,
            AvatarId = role == UserRole.Student ? defaultAvatar?.Id : null,
            TotalXp = 0
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(jwtTokenService.CreateToken(user), ToDto(user));
    }

    internal static AuthUserDto ToDto(User user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            user.ParentId,
            user.AvatarId,
            user.TotalXp,
            user.MobilePhone,
            user.WorkShift?.ToString(),
            user.TenantId);

    /// <summary>Keeps digits and an optional leading +.</summary>
    internal static string NormalizePhone(string? phone)
    {
        var raw = (phone ?? string.Empty).Trim();
        if (raw.Length == 0) return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsDigit(c) || (c == '+' && sb.Length == 0))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
