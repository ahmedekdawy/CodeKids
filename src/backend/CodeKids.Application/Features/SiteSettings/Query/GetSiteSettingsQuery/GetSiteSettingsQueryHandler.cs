using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public sealed class GetSiteSettingsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(GetSiteSettingsQuery query, CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(dbContext, cancellationToken);
        return SiteSettingsMapper.ToDto(settings);
    }

    public static async Task<Domain.Entities.SiteSettings> EnsureAsync(
        IAppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenantId = dbContext.CurrentTenantId;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var tenantSettings = await dbContext.SiteSettings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
            if (tenantSettings is not null)
            {
                return tenantSettings;
            }

            var template = await dbContext.SiteSettings
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.TenantId == null && x.Id == Domain.Entities.SiteSettings.DefaultId, cancellationToken)
                ?? await dbContext.SiteSettings
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.TenantId == null, cancellationToken);

            tenantSettings = CloneForTenant(template, tenantId);
            dbContext.SiteSettings.Add(tenantSettings);
            await dbContext.SaveChangesAsync(cancellationToken);
            return tenantSettings;
        }

        var settings = await dbContext.SiteSettings
            .FirstOrDefaultAsync(x => x.Id == Domain.Entities.SiteSettings.DefaultId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new Domain.Entities.SiteSettings
        {
            Id = Domain.Entities.SiteSettings.DefaultId,
            SiteName = "CodeKids",
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.SiteSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static Domain.Entities.SiteSettings CloneForTenant(Domain.Entities.SiteSettings? template, string tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SiteName = template?.SiteName ?? "CodeKids",
            LogoStorageKey = template?.LogoStorageKey ?? string.Empty,
            LogoContentType = template?.LogoContentType ?? string.Empty,
            BannerStorageKey = template?.BannerStorageKey ?? string.Empty,
            BannerContentType = template?.BannerContentType ?? string.Empty,
            TimetableWeekStartUtc = template?.TimetableWeekStartUtc,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
