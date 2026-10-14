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
            string.IsNullOrWhiteSpace(settings.LogoStorageKey) ? null : ImageUrl("logo", settings.TenantId),
            string.IsNullOrWhiteSpace(settings.BannerStorageKey) ? null : ImageUrl("banner", settings.TenantId),
            settings.TimetableWeekStartUtc,
            Domain.Entities.SiteSettings.NormalizeSessionCount(settings.AmSessionCount),
            Domain.Entities.SiteSettings.NormalizeSessionCount(settings.PmSessionCount),
            Domain.Entities.SiteSettings.NormalizePmStartMinutes(settings.PmStartMinutes),
            settings.UpdatedAtUtc,
            settings.TenantStyle.ToString(),
            settings.MultiTimeZone,
            settings.TimeZone);

    /// <summary>
    /// The tenant goes in the URL because an image request cannot carry the tenant header; without it
    /// the API would serve whichever tenant its own address maps to.
    /// </summary>
    private static string ImageUrl(string kind, string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId)
            ? $"/api/site-settings/{kind}"
            : $"/api/site-settings/{kind}?tenant={Uri.EscapeDataString(tenantId)}";
}
