using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record WatchSessionDto(
    Guid Id,
    Guid MediaAssetId,
    Guid StudentId,
    string StudentName,
    Guid? LessonId,
    int ActualWatchSeconds,
    int MaxPositionSeconds,
    bool UsedSpeedUp,
    bool SkippedAhead,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastEventAtUtc);
