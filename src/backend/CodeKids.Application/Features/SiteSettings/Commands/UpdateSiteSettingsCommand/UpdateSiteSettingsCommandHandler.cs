using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
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

        if (command.AmSessionCount.HasValue)
        {
            settings.AmSessionCount = await ValidateSessionCountAsync(
                dbContext,
                TimetablePeriod.Am,
                command.AmSessionCount.Value,
                cancellationToken);
        }

        if (command.PmSessionCount.HasValue)
        {
            settings.PmSessionCount = await ValidateSessionCountAsync(
                dbContext,
                TimetablePeriod.Pm,
                command.PmSessionCount.Value,
                cancellationToken);
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

    private static async Task<int> ValidateSessionCountAsync(
        IAppDbContext dbContext,
        TimetablePeriod period,
        int count,
        CancellationToken cancellationToken)
    {
        if (count is < Domain.Entities.SiteSettings.MinSessionCount or > Domain.Entities.SiteSettings.MaxSessionCount)
        {
            throw new InvalidOperationException(
                $"Timetable session count must be between {Domain.Entities.SiteSettings.MinSessionCount} and {Domain.Entities.SiteSettings.MaxSessionCount}.");
        }

        var periodLabel = period == TimetablePeriod.Pm ? "PM" : "AM";
        var existsBeyond = await dbContext.FixedTimetableEntries
            .AsNoTracking()
            .AnyAsync(x => x.Period == period && x.SessionNumber > count, cancellationToken);
        if (existsBeyond)
        {
            throw new InvalidOperationException(
                $"Cannot reduce {periodLabel} sessions while timetable entries exist beyond session {count}.");
        }

        return count;
    }
}
