using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SiteSettings;

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

        if (command.ClearTimetableWeek)
        {
            settings.TimetableWeekStartUtc = null;
        }
        else if (command.TimetableWeekStartUtc.HasValue)
        {
            settings.TimetableWeekStartUtc = NormalizeTimetableWeekStart(command.TimetableWeekStartUtc.Value);
        }

        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return SiteSettingsMapper.ToDto(settings);
    }

    private static DateTimeOffset NormalizeTimetableWeekStart(DateTimeOffset value)
    {
        var date = value.UtcDateTime.Date;
        var sunday = date.AddDays(-(int)date.DayOfWeek);
        return new DateTimeOffset(sunday, TimeSpan.Zero);
    }
}
