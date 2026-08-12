using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record StudentLevelDto(
    int LevelNumber,
    string Code,
    string Name,
    int MinXp,
    int? NextMinXp,
    int ProgressPercent);
