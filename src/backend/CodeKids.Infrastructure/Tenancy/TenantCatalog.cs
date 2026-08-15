using Microsoft.Extensions.Configuration;

namespace CodeKids.Infrastructure.Tenancy;

public sealed record TenantInfo(string Id, string ConnectionString, IReadOnlyList<string> Hosts);

public sealed class TenantCatalog
{
    public const string HeaderName = "X-Tenant-Id";

    public bool Enabled { get; }
    public TenantInfo Default { get; }
    public IReadOnlyList<TenantInfo> All { get; }

    public TenantCatalog(IConfiguration configuration)
    {
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        Enabled = !string.Equals(configuration["Tenants:Enabled"], "false", StringComparison.OrdinalIgnoreCase);

        var items = new List<TenantInfo>();
        foreach (var child in configuration.GetSection("Tenants:Items").GetChildren())
        {
            var id = (child["Id"] ?? child.Key).Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var connectionName = child["ConnectionStringName"];
            var connection = string.IsNullOrWhiteSpace(connectionName)
                ? child["ConnectionString"]
                : configuration.GetConnectionString(connectionName);

            if (string.IsNullOrWhiteSpace(connection))
            {
                continue;
            }

            var hosts = child.GetSection("Hosts").GetChildren()
                .Select(x => NormalizeHost(x.Value))
                .Where(x => x is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            items.Add(new TenantInfo(id, connection, hosts));
        }

        if (items.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(defaultConnection))
            {
                items.Add(new TenantInfo("abakera", defaultConnection, ["localhost", "abakera.runasp.net", "abakeraadmin.runasp.net", "www.abakeraadmin.runasp.net"]));
            }

            var esraa = configuration.GetConnectionString("EsraaConnection");
            if (!string.IsNullOrWhiteSpace(esraa))
            {
                items.Add(new TenantInfo("esraa", esraa, []));
            }
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("No tenant connection strings are configured.");
        }

        All = items;
        var defaultId = configuration["Tenants:Default"] ?? items[0].Id;
        Default = items.FirstOrDefault(x => string.Equals(x.Id, defaultId, StringComparison.OrdinalIgnoreCase))
            ?? items[0];
    }

    public TenantInfo Resolve(string? tenantHeader, string? origin, string? requestHost)
    {
        if (!Enabled)
        {
            return Default;
        }

        if (!string.IsNullOrWhiteSpace(tenantHeader))
        {
            var byId = FindById(tenantHeader);
            if (byId is not null)
            {
                return byId;
            }
        }

        var originHost = HostFromOrigin(origin);
        var byOrigin = FindByHost(originHost);
        if (byOrigin is not null)
        {
            return byOrigin;
        }

        var byRequestHost = FindByHost(NormalizeHost(requestHost));
        if (byRequestHost is not null)
        {
            return byRequestHost;
        }

        return Default;
    }

    public TenantInfo? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return All.FirstOrDefault(x => string.Equals(x.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> FrontendOrigins()
    {
        foreach (var tenant in All)
        {
            foreach (var host in tenant.Hosts)
            {
                if (host is "localhost" or "127.0.0.1")
                {
                    yield return "http://localhost:4200";
                    yield return "https://localhost:4200";
                    continue;
                }

                yield return $"http://{host}";
                yield return $"https://{host}";
            }
        }
    }

    private TenantInfo? FindByHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return All.FirstOrDefault(t =>
            t.Hosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? HostFromOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            ? NormalizeHost(uri.Host)
            : NormalizeHost(origin);
    }

    private static string? NormalizeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var host = value.Trim().Trim().TrimEnd('/');
        var slash = host.IndexOf('/');
        if (slash >= 0)
        {
            host = host[..slash];
        }

        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            host = host["http://".Length..];
        }
        else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            host = host["https://".Length..];
        }

        var colon = host.IndexOf(':');
        if (colon >= 0)
        {
            host = host[..colon];
        }

        return string.IsNullOrWhiteSpace(host) ? null : host;
    }
}
