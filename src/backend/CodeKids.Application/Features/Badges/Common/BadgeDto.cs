using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Badges;

public sealed record BadgeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Icon,
    int RequiredXp,
    int RequiredSteps,
    bool IsEarned);
