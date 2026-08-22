namespace CodeKids.Infrastructure.Tenancy;

public interface ITenantContext
{
    string TenantId { get; }
}
