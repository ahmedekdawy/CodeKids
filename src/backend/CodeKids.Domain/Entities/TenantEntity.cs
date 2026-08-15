namespace CodeKids.Domain.Entities;

public abstract class TenantEntity
{
    /// <summary>Null means the row is shared with every tenant.</summary>
    public string? TenantId { get; set; }
}
