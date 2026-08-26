namespace CodeKids.Domain.Entities;

public class SiteSettings : TenantEntity
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public const int DefaultSessionCount = 6;
    public const int MinSessionCount = 1;
    public const int MaxSessionCount = 12;
    public const int DefaultPmStartMinutes = 15 * 60;
    public const int MinPmStartMinutes = 12 * 60;
    public const int MaxPmStartMinutes = 21 * 60;

    public Guid Id { get; set; } = DefaultId;
    public string SiteName { get; set; } = "CodeKids";
    public string LogoStorageKey { get; set; } = string.Empty;
    public string LogoContentType { get; set; } = string.Empty;
    public string BannerStorageKey { get; set; } = string.Empty;
    public string BannerContentType { get; set; } = string.Empty;
    public DateTimeOffset? TimetableWeekStartUtc { get; set; }
    public int AmSessionCount { get; set; } = DefaultSessionCount;
    public int PmSessionCount { get; set; } = DefaultSessionCount;
    /// <summary>Minutes from midnight when the first PM session starts. Default 15:00.</summary>
    public int PmStartMinutes { get; set; } = DefaultPmStartMinutes;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static int NormalizeSessionCount(int value) =>
        Math.Clamp(value, MinSessionCount, MaxSessionCount);

    public static int NormalizePmStartMinutes(int value) =>
        value <= 0
            ? DefaultPmStartMinutes
            : Math.Clamp(value, MinPmStartMinutes, MaxPmStartMinutes);
}
