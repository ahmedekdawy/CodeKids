using Microsoft.AspNetCore.Http;

namespace CodeKids.Infrastructure.Tenancy;

public sealed record TenantResolution(string TenantId, string ConnectionString);

public static class TenantRequest
{
    public const string HttpItemKey = "CodeKids.Tenant";

    public static TenantResolution Resolve(HttpContext? http, TenantCatalog catalog)
    {
        if (http is null)
        {
            return new TenantResolution(catalog.Default.Id, catalog.Default.ConnectionString);
        }

        if (http.Items.TryGetValue(HttpItemKey, out var cached) && cached is TenantResolution info)
        {
            return info;
        }

        var header = http.Request.Headers[TenantCatalog.HeaderName].ToString().Trim();
        var connection = catalog.Resolve(header, http.Request.Headers.Origin.ToString(), http.Request.Host.Host);
        var tenantId = SanitizeTenantId(header) ?? connection.Id;
        var resolved = new TenantResolution(tenantId, connection.ConnectionString);
        http.Items[HttpItemKey] = resolved;
        return resolved;
    }

    private static string? SanitizeTenantId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var slug = value.Trim().ToLowerInvariant();
        return slug.Length > 64 ? slug[..64] : slug;
    }
}

public sealed class HttpTenantContext(IHttpContextAccessor accessor, TenantCatalog catalog) : ITenantContext
{
    public string TenantId => TenantRequest.Resolve(accessor.HttpContext, catalog).TenantId;
}
