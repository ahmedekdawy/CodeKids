using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

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
