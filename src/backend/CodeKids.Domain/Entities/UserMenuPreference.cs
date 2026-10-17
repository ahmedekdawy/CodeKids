namespace CodeKids.Domain.Entities;

/// <summary>
/// One user's adjustment of a page in their own menu: its position and whether it is hidden.
/// Only pages their roles expose can have a preference row.
/// </summary>
public class UserMenuPreference : TenantEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    public int SortOrder { get; set; }

    public bool IsHidden { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
