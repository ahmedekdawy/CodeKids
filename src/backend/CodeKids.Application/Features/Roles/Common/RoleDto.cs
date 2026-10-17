using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Roles;

public sealed record PageDto(
    Guid Id,
    string Key,
    string LabelKey,
    string Icon,
    string CategoryKey,
    string Panel,
    int SortOrder);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string DisplayName,
    string Description,
    bool IsSystem,
    string BaseRole,
    int MemberCount,
    IReadOnlyList<PageDto> Pages);

public sealed record RoleMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset AssignedAtUtc);

internal static class RoleMapper
{
    public static PageDto ToDto(Page page) => new(
        page.Id,
        page.Key,
        page.LabelKey,
        page.Icon,
        page.CategoryKey,
        page.Panel,
        page.SortOrder);

    public static RoleDto ToDto(Role role, int memberCount) => new(
        role.Id,
        role.Name,
        role.DisplayName,
        role.Description,
        role.IsSystem,
        role.BaseRole.ToString(),
        memberCount,
        role.Pages
            .Where(x => x.Page is not null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Page!.SortOrder)
            .Select(x => ToDto(x.Page!))
            .ToList());
}

internal static class RoleAdminGuard
{
    public static async Task EnsureAdminAsync(
        IAppDbContext dbContext,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        _ = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == adminUserId && (x.Role == UserRole.SuperAdmin || x.IsTenantOwner),
                cancellationToken)
            ?? throw new InvalidOperationException("Super Admin account not found.");
    }

    public static UserRole ParseBaseRole(string? raw)
    {
        if (Enum.TryParse<UserRole>((raw ?? string.Empty).Trim(), ignoreCase: true, out var role)
            && role is UserRole.Teacher or UserRole.SuperAdmin)
        {
            return role;
        }

        throw new InvalidOperationException("Role type must be Teacher or SuperAdmin.");
    }

    public static string ParseName(string? raw)
    {
        var name = (raw ?? string.Empty).Trim();
        if (name.Length is < 2 or > 60)
        {
            throw new InvalidOperationException("Role name must be between 2 and 60 characters.");
        }

        if (name.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not ' ' and not '-' and not '_'))
        {
            throw new InvalidOperationException("Role name can only contain letters, digits, spaces, dashes, and underscores.");
        }

        return name;
    }

    public static string ParseDisplayName(string? raw)
    {
        var displayName = (raw ?? string.Empty).Trim();
        if (displayName.Length is < 1 or > 120)
        {
            throw new InvalidOperationException("Role display name must be between 1 and 120 characters.");
        }

        return displayName;
    }

    public static string ParseDescription(string? raw)
    {
        var description = (raw ?? string.Empty).Trim();
        if (description.Length > 300)
        {
            throw new InvalidOperationException("Role description must be 300 characters or fewer.");
        }

        return description;
    }

    /// <summary>Returns the pages in catalogue order, rejecting keys from the other panel.</summary>
    public static async Task<List<Page>> ResolvePagesAsync(
        IAppDbContext dbContext,
        UserRole baseRole,
        IEnumerable<string>? keys,
        CancellationToken cancellationToken)
    {
        var panel = baseRole == UserRole.SuperAdmin ? Page.AdminPanel : Page.TeacherPanel;
        var requested = (keys ?? [])
            .Select(key => (key ?? string.Empty).Trim())
            .Where(key => key.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count == 0)
        {
            throw new InvalidOperationException("Choose at least one page for the role.");
        }

        var pages = await dbContext.Pages
            .Where(x => x.Panel == panel)
            .ToListAsync(cancellationToken);
        var byKey = pages.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        if (requested.Any(key => !byKey.ContainsKey(key)))
        {
            throw new InvalidOperationException("A selected page does not belong to the role's panel.");
        }

        return requested
            .Select(key => byKey[key])
            .OrderBy(page => page.SortOrder)
            .ToList();
    }
}

public sealed record ListRolesQuery(string? BaseRole = null) : CodeKids.Domain.Abstractions.IQuery<IReadOnlyList<RoleDto>>;

public sealed class ListRolesQueryHandler(IAppDbContext dbContext)
    : CodeKids.Domain.Abstractions.IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = dbContext.Roles
            .AsNoTracking()
            .Include(x => x.Pages)
            .ThenInclude(x => x.Page)
            .Include(x => x.Members)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.BaseRole)
            && Enum.TryParse<UserRole>(query.BaseRole, true, out var baseRole))
        {
            roles = roles.Where(x => x.BaseRole == baseRole);
        }

        return (await roles
                .OrderBy(x => x.BaseRole)
                .ThenByDescending(x => x.IsSystem)
                .ThenBy(x => x.DisplayName)
                .ToListAsync(cancellationToken))
            .Select(role => RoleMapper.ToDto(role, role.Members.Count))
            .ToList();
    }
}

public sealed record ListRoleMembersQuery(Guid RoleId) : CodeKids.Domain.Abstractions.IQuery<IReadOnlyList<RoleMemberDto>>;

public sealed class ListRoleMembersQueryHandler(IAppDbContext dbContext)
    : CodeKids.Domain.Abstractions.IQueryHandler<ListRoleMembersQuery, IReadOnlyList<RoleMemberDto>>
{
    public async Task<IReadOnlyList<RoleMemberDto>> Handle(
        ListRoleMembersQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserInRoles
            .AsNoTracking()
            .Where(x => x.RoleId == query.RoleId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => new RoleMemberDto(
                x.UserId,
                x.User!.DisplayName,
                x.User.Email,
                x.User.Role.ToString(),
                x.User.IsActive,
                x.AssignedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

public sealed record ListPagesQuery : CodeKids.Domain.Abstractions.IQuery<IReadOnlyList<PageDto>>;

public sealed class ListPagesQueryHandler(IAppDbContext dbContext)
    : CodeKids.Domain.Abstractions.IQueryHandler<ListPagesQuery, IReadOnlyList<PageDto>>
{
    public async Task<IReadOnlyList<PageDto>> Handle(ListPagesQuery query, CancellationToken cancellationToken)
    {
        return (await dbContext.Pages
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken))
            .Select(RoleMapper.ToDto)
            .ToList();
    }
}
