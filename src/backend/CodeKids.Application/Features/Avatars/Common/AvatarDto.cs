using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

public sealed record AvatarDto(
    Guid Id,
    string Name,
    string Theme,
    string AccentColor,
    string Emoji,
    int UnlockXp,
    bool IsUnlocked,
    bool IsSelected);
