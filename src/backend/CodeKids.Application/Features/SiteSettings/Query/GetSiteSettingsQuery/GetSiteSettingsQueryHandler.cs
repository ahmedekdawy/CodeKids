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
}
