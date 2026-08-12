using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public static class SiteSettingsMapper
{
    public static SiteSettingsDto ToDto(Domain.Entities.SiteSettings settings) =>
        new(
            settings.SiteName,
            string.IsNullOrWhiteSpace(settings.LogoStorageKey) ? null : "/api/site-settings/logo",
            string.IsNullOrWhiteSpace(settings.BannerStorageKey) ? null : "/api/site-settings/banner",
            settings.TimetableWeekStartUtc,
            settings.UpdatedAtUtc);
}
