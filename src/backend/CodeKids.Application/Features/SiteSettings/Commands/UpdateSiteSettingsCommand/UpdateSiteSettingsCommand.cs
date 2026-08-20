using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public sealed record UpdateSiteSettingsRequest(
    string SiteName,
    bool? ClearLogo = null,
    bool? ClearBanner = null,
    DateTimeOffset? TimetableWeekStartUtc = null,
    bool? ClearTimetableWeek = null,
    int? AmSessionCount = null,
    int? PmSessionCount = null);

public sealed record UpdateSiteSettingsCommand(
    Guid AdminUserId,
    string SiteName,
    bool ClearLogo,
    bool ClearBanner,
    DateTimeOffset? TimetableWeekStartUtc,
    bool ClearTimetableWeek,
    int? AmSessionCount,
    int? PmSessionCount) : ICommand<SiteSettingsDto>;
