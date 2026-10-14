namespace CodeKids.Domain.Entities;

public class TenantSignup : TenantEntity
{
    public Guid Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public Enums.TenantStyle TenantStyle { get; set; } = Enums.TenantStyle.School;
    /// <summary>When true, students and teachers each have their own time zone and timetables are shown in it.</summary>
    public bool MultiTimeZone { get; set; }
    /// <summary>IANA id of the zone the tenant's timetable times are written in; null when never set.</summary>
    public string? TimeZone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
