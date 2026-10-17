using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Menu;

public sealed record MenuPageDto(
    Guid PageId,
    string Key,
    string LabelKey,
    string Icon,
    string CategoryKey,
    string Panel,
    int SortOrder,
    bool IsHidden);

public sealed record MyMenuDto(IReadOnlyList<MenuPageDto> Items, IReadOnlyList<string> Roles);

public sealed record GetMyMenuQuery(Guid UserId) : IQuery<MyMenuDto>;

/// <summary>
/// Resolves the pages a user's menu may contain: the union of their assigned roles, falling back to
/// the default role for their base role when they have no assignment yet. The owner of a
/// single-teacher tenant also gets the default admin role because they have admin rights.
/// </summary>
internal static class MenuResolver
{
    public static async Task<(User User, IReadOnlyList<Role> Roles, IReadOnlyList<Page> Pages)> ResolveAsync(
        IAppDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var roleIds = await dbContext.UserInRoles
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await dbContext.Roles
            .Include(x => x.Pages)
            .ThenInclude(x => x.Page)
            .Where(x => roleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            var defaultRole = await dbContext.Roles
                .Include(x => x.Pages)
                .ThenInclude(x => x.Page)
                .FirstOrDefaultAsync(x => x.IsSystem && x.BaseRole == user.Role, cancellationToken);
            if (defaultRole is not null)
            {
                roles.Add(defaultRole);
            }
        }

        if (user.IsTenantOwner && roles.All(x => x.Name != Role.SuperAdminName))
        {
            var adminRole = await dbContext.Roles
                .Include(x => x.Pages)
                .ThenInclude(x => x.Page)
                .FirstOrDefaultAsync(x => x.IsSystem && x.Name == Role.SuperAdminName, cancellationToken);
            if (adminRole is not null)
            {
                roles.Add(adminRole);
            }
        }

        var pages = roles
            .SelectMany(role => role.Pages)
            .Where(link => link.Page is not null)
            .Select(link => link.Page!)
            .GroupBy(page => page.Id)
            .Select(group => group.First())
            .ToList();

        return (user, roles, pages);
    }

    public static MyMenuDto ToDto(IReadOnlyList<Role> roles, IReadOnlyList<Page> pages, IReadOnlyList<UserMenuPreference> preferences)
    {
        var byPage = preferences.ToDictionary(x => x.PageId);
        var items = pages
            .Select(page =>
            {
                byPage.TryGetValue(page.Id, out var preference);
                return new
                {
                    Page = page,
                    Order = preference?.SortOrder ?? int.MaxValue,
                    Hidden = preference?.IsHidden == true
                };
            })
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Page.SortOrder)
            .Select(x => new MenuPageDto(
                x.Page.Id,
                x.Page.Key,
                x.Page.LabelKey,
                x.Page.Icon,
                x.Page.CategoryKey,
                x.Page.Panel,
                x.Page.SortOrder,
                x.Hidden))
            .ToList();

        var roleNames = roles
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.DisplayName)
            .Select(x => x.DisplayName)
            .ToList();

        return new MyMenuDto(items, roleNames);
    }
}

public sealed class GetMyMenuQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetMyMenuQuery, MyMenuDto>
{
    public async Task<MyMenuDto> Handle(GetMyMenuQuery query, CancellationToken cancellationToken)
    {
        var (user, roles, pages) = await MenuResolver.ResolveAsync(dbContext, query.UserId, cancellationToken);
        var preferences = await dbContext.UserMenuPreferences
            .Where(x => x.UserId == user.Id)
            .ToListAsync(cancellationToken);
        return MenuResolver.ToDto(roles, pages, preferences);
    }
}
