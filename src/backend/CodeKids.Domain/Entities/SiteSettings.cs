namespace CodeKids.Domain.Entities;

public class SiteSettings : TenantEntity
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public const int DefaultSessionCount = 6;
    public const int MinSessionCount = 1;
    public const int MaxSessionCount = 12;

    public Guid Id { get; set; } = DefaultId;
    public string SiteName { get; set; } = "CodeKids";
    public string LogoStorageKey { get; set; } = string.Empty;
    public string LogoContentType { get; set; } = string.Empty;
    public string BannerStorageKey { get; set; } = string.Empty;
    public string BannerContentType { get; set; } = string.Empty;
    public DateTimeOffset? TimetableWeekStartUtc { get; set; }
    public int AmSessionCount { get; set; } = DefaultSessionCount;
    public int PmSessionCount { get; set; } = DefaultSessionCount;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static int NormalizeSessionCount(int value) =>
        Math.Clamp(value, MinSessionCount, MaxSessionCount);
}
