using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record WatchSummaryDto(
    Guid MediaAssetId,
    Guid? LessonId,
    string? LessonTitle,
    int ActualWatchSeconds,
    bool UsedSpeedUp,
    bool SkippedAhead,
    DateTimeOffset LastEventAtUtc);
