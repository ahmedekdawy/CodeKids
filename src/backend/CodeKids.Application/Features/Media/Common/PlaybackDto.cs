using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record PlaybackDto(
    Guid MediaAssetId,
    string PlaybackUrl,
    string WatermarkText,
    DateTimeOffset ExpiresAtUtc,
    int? DurationSeconds,
    string ContentType,
    string FileName,
    bool IsExternalLink = false);
