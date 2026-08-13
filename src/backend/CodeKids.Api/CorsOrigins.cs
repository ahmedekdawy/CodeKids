namespace CodeKids.Api;

internal static class CorsOrigins
{
    /// <summary>
    /// Known SPA origins. CI does not publish appsettings.json, so production
    /// hosts cannot rely on Cors:AllowedOrigins existing on the server.
    /// </summary>
    private static readonly string[] BuiltIn =
    [
        "http://localhost:4200",
        "https://localhost:4200",
        "http://abakeraadmin.runasp.net",
        "https://abakeraadmin.runasp.net",
        "http://www.abakeraadmin.runasp.net",
        "https://www.abakeraadmin.runasp.net"
    ];

    public static HashSet<string> Resolve(IConfiguration configuration)
    {
        var fromConfig = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var origin in BuiltIn.Concat(fromConfig))
        {
            var normalized = Normalize(origin);
            if (normalized is not null)
            {
                set.Add(normalized);
            }
        }

        return set;
    }

    public static bool IsAllowed(HashSet<string> allowed, string? origin)
    {
        var normalized = Normalize(origin);
        return normalized is not null && allowed.Contains(normalized);
    }

    private static string? Normalize(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        return origin.Trim().TrimEnd('/');
    }
}
