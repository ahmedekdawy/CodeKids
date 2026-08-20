using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public sealed record SiteSettingsDto(
    string SiteName,
    string? LogoUrl,
    string? BannerUrl,
    DateTimeOffset? TimetableWeekStartUtc,
    int AmSessionCount,
    int PmSessionCount,
    DateTimeOffset UpdatedAtUtc);
