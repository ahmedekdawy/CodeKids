namespace CodeKids.Infrastructure.Tenancy;

public sealed class FixedTenantContext(string tenantId) : ITenantContext
{
    public string TenantId { get; } = tenantId;
}
