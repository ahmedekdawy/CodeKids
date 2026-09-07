using CodeKids.Infrastructure.Tenancy;

namespace CodeKids.Api;

internal static class MediaApiBaseUrl
{
    internal static string Resolve(
        string? apiBase,
        TenantInfo tenant,
        string? configuredPublicBaseUrl,
        HttpContext httpContext)
    {
        if (!string.IsNullOrWhiteSpace(apiBase))
        {
            return apiBase.TrimEnd('/');
        }

        if (!string.IsNullOrWhiteSpace(tenant.ApiBaseUrl))
        {
            return tenant.ApiBaseUrl.TrimEnd('/');
        }

        if (!string.IsNullOrWhiteSpace(configuredPublicBaseUrl))
        {
            return configuredPublicBaseUrl.TrimEnd('/');
        }

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api".TrimEnd('/');
    }
}
