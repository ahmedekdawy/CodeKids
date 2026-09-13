using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Tenants;

public sealed class ListTenantsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTenantsQuery, IReadOnlyList<AdminTenantDto>>
{
    public async Task<IReadOnlyList<AdminTenantDto>> Handle(
        ListTenantsQuery query,
        CancellationToken cancellationToken)
    {
        var signups = await dbContext.TenantSignups
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var userStats = await dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId != null && x.TenantId != "")
            .GroupBy(x => x.TenantId!)
            .Select(g => new
            {
                TenantId = g.Key,
                UserCount = g.Count(),
                LastLoginUtc = g.Max(x => x.LastLoginDateUtc)
            })
            .ToListAsync(cancellationToken);

        var statsById = userStats.ToDictionary(
            x => x.TenantId,
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var rows = new List<AdminTenantDto>(signups.Count + userStats.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var signup in signups)
        {
            var id = string.IsNullOrWhiteSpace(signup.TenantSlug) ? signup.Id.ToString() : signup.TenantSlug;
            seen.Add(id);
            statsById.TryGetValue(id, out var stats);

            var status = signup.VerifiedAtUtc is not null
                ? "Verified"
                : signup.ExpiresAtUtc < DateTimeOffset.UtcNow
                    ? "Expired"
                    : "Pending";

            rows.Add(new AdminTenantDto(
                id,
                signup.TenantName,
                signup.TenantSlug,
                signup.Email,
                signup.MobilePhone,
                signup.DisplayName,
                status,
                signup.CreatedAtUtc,
                signup.VerifiedAtUtc,
                signup.ExpiresAtUtc,
                stats?.UserCount ?? 0,
                stats?.LastLoginUtc));
        }

        foreach (var stats in userStats.OrderBy(x => x.TenantId, StringComparer.OrdinalIgnoreCase))
        {
            if (!seen.Add(stats.TenantId))
            {
                continue;
            }

            rows.Add(new AdminTenantDto(
                stats.TenantId,
                stats.TenantId,
                stats.TenantId,
                "",
                "",
                "",
                "Active",
                null,
                null,
                null,
                stats.UserCount,
                stats.LastLoginUtc));
        }

        return rows
            .OrderByDescending(x => x.CreatedAtUtc ?? x.LastLoginUtc ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.Slug, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
