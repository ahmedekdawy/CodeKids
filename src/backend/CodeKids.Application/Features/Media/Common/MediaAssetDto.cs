using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record MediaAssetDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int? DurationSeconds,
    DateTimeOffset CreatedAtUtc,
    string? ExternalUrl = null);
