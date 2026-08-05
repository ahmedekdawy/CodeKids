using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

public sealed record SiteSettingsDto(
    string SiteName,
    string? LogoUrl,
    string? BannerUrl,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateSiteSettingsRequest(string SiteName, bool? ClearLogo = null, bool? ClearBanner = null);

public sealed record GetSiteSettingsQuery() : IQuery<SiteSettingsDto>;

public sealed record UpdateSiteSettingsCommand(
    Guid AdminUserId,
    string SiteName,
    bool ClearLogo,
    bool ClearBanner) : ICommand<SiteSettingsDto>;

public sealed record UploadSiteImageCommand(
    Guid AdminUserId,
    string Kind,
    string StorageKey,
    string ContentType) : ICommand<SiteSettingsDto>;

public static class SiteSettingsMapper
{
    public static SiteSettingsDto ToDto(Domain.Entities.SiteSettings settings) =>
        new(
            settings.SiteName,
            string.IsNullOrWhiteSpace(settings.LogoStorageKey) ? null : "/api/site-settings/logo",
            string.IsNullOrWhiteSpace(settings.BannerStorageKey) ? null : "/api/site-settings/banner",
            settings.UpdatedAtUtc);
}

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

public sealed class UpdateSiteSettingsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(UpdateSiteSettingsCommand command, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && x.Role == Domain.Enums.UserRole.SuperAdmin, cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");

        var name = command.SiteName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Site name is required.");
        }

        var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);
        settings.SiteName = name;
        if (command.ClearLogo)
        {
            settings.LogoStorageKey = string.Empty;
            settings.LogoContentType = string.Empty;
        }

        if (command.ClearBanner)
        {
            settings.BannerStorageKey = string.Empty;
            settings.BannerContentType = string.Empty;
        }

        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return SiteSettingsMapper.ToDto(settings);
    }
}

public sealed class UploadSiteImageCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UploadSiteImageCommand, SiteSettingsDto>
{
    public static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif",
        "image/svg+xml"
    };

    public async Task<SiteSettingsDto> Handle(UploadSiteImageCommand command, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.AdminUserId && x.Role == Domain.Enums.UserRole.SuperAdmin, cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");

        if (!AllowedImageTypes.Contains(command.ContentType))
        {
            throw new InvalidOperationException("Only PNG, JPEG, WebP, GIF, or SVG images are allowed.");
        }

        var kind = command.Kind.Trim().ToLowerInvariant();
        if (kind is not ("logo" or "banner"))
        {
            throw new InvalidOperationException("Image kind must be logo or banner.");
        }

        var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);
        if (kind == "logo")
        {
            settings.LogoStorageKey = command.StorageKey;
            settings.LogoContentType = command.ContentType;
        }
        else
        {
            settings.BannerStorageKey = command.StorageKey;
            settings.BannerContentType = command.ContentType;
        }

        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return SiteSettingsMapper.ToDto(settings);
    }
}
