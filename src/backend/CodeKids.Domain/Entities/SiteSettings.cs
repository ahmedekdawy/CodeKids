namespace CodeKids.Domain.Entities;

public class SiteSettings
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public Guid Id { get; set; } = DefaultId;
    public string SiteName { get; set; } = "CodeKids";
    public string LogoStorageKey { get; set; } = string.Empty;
    public string LogoContentType { get; set; } = string.Empty;
    public string BannerStorageKey { get; set; } = string.Empty;
    public string BannerContentType { get; set; } = string.Empty;
    public DateTimeOffset? TimetableWeekStartUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
