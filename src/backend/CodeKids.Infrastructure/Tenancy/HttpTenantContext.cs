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
        var resolved = new TenantResolution(connection.Id, connection.ConnectionString);
        http.Items[HttpItemKey] = resolved;
        return resolved;
    }
}

public sealed class HttpTenantContext(IHttpContextAccessor accessor, TenantCatalog catalog) : ITenantContext
{
    public string TenantId => TenantRequest.Resolve(accessor.HttpContext, catalog).TenantId;
}
