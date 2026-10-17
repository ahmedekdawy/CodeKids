using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Roles;

public sealed record CreateRoleRequest(
    string Name,
    string DisplayName,
    string? Description,
    string BaseRole,
    IReadOnlyList<string>? PageKeys);

public sealed record CreateRoleCommand(
    Guid AdminUserId,
    string Name,
    string DisplayName,
    string? Description,
    string BaseRole,
    IReadOnlyList<string>? PageKeys) : ICommand<RoleDto>;

public sealed class CreateRoleCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        await RoleAdminGuard.EnsureAdminAsync(dbContext, command.AdminUserId, cancellationToken);

        var name = RoleAdminGuard.ParseName(command.Name);
        var displayName = RoleAdminGuard.ParseDisplayName(command.DisplayName);
        var description = RoleAdminGuard.ParseDescription(command.Description);
        var baseRole = RoleAdminGuard.ParseBaseRole(command.BaseRole);

        if (await dbContext.Roles.AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            throw new InvalidOperationException("A role with that name already exists.");
        }

        var pages = await RoleAdminGuard.ResolvePagesAsync(
            dbContext,
            baseRole,
            command.PageKeys,
            cancellationToken);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsSystem = false,
            BaseRole = baseRole,
            Pages = pages
                .Select((page, index) => new RolePage
                {
                    PageId = page.Id,
                    Page = page,
                    SortOrder = index
                })
                .ToList()
        };

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RoleMapper.ToDto(role, 0);
    }
}

public sealed record UpdateRoleRequest(
    string DisplayName,
    string? Description,
    IReadOnlyList<string>? PageKeys);

public sealed record UpdateRoleCommand(
    Guid AdminUserId,
    Guid RoleId,
    string DisplayName,
    string? Description,
    IReadOnlyList<string>? PageKeys) : ICommand<RoleDto>;

public sealed class UpdateRoleCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        await RoleAdminGuard.EnsureAdminAsync(dbContext, command.AdminUserId, cancellationToken);

        var role = await dbContext.Roles
            .Include(x => x.Pages)
            .ThenInclude(x => x.Page)
            .FirstOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        if (role.IsSystem)
        {
            throw new InvalidOperationException("Default roles cannot be edited.");
        }

        var pages = await RoleAdminGuard.ResolvePagesAsync(
            dbContext,
            role.BaseRole,
            command.PageKeys,
            cancellationToken);
        var desiredIds = pages.Select(x => x.Id).ToHashSet();

        foreach (var stale in role.Pages.Where(x => !desiredIds.Contains(x.PageId)).ToList())
        {
            dbContext.RolePages.Remove(stale);
            role.Pages.Remove(stale);
        }

        for (var index = 0; index < pages.Count; index++)
        {
            var link = role.Pages.FirstOrDefault(x => x.PageId == pages[index].Id);
            if (link is null)
            {
                link = new RolePage { PageId = pages[index].Id, Page = pages[index] };
                role.Pages.Add(link);
            }

            link.SortOrder = index;
        }

        role.DisplayName = RoleAdminGuard.ParseDisplayName(command.DisplayName);
        role.Description = RoleAdminGuard.ParseDescription(command.Description);

        await dbContext.SaveChangesAsync(cancellationToken);

        var memberCount = await dbContext.UserInRoles.CountAsync(x => x.RoleId == role.Id, cancellationToken);
        return RoleMapper.ToDto(role, memberCount);
    }
}

public sealed record DeleteRoleCommand(Guid AdminUserId, Guid RoleId) : ICommand<bool>;

public sealed class DeleteRoleCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        await RoleAdminGuard.EnsureAdminAsync(dbContext, command.AdminUserId, cancellationToken);

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        if (role.IsSystem)
        {
            throw new InvalidOperationException("Default roles cannot be deleted.");
        }

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record AssignRoleUserCommand(Guid AdminUserId, Guid RoleId, Guid UserId) : ICommand<RoleDto>;

public sealed class AssignRoleUserCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AssignRoleUserCommand, RoleDto>
{
    public async Task<RoleDto> Handle(AssignRoleUserCommand command, CancellationToken cancellationToken)
    {
        await RoleAdminGuard.EnsureAdminAsync(dbContext, command.AdminUserId, cancellationToken);

        var role = await dbContext.Roles
            .Include(x => x.Pages)
            .ThenInclude(x => x.Page)
            .FirstOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!user.IsTenantOwner && user.Role != role.BaseRole)
        {
            throw new InvalidOperationException(
                $"Only {role.BaseRole} accounts can be added to this role.");
        }

        var exists = await dbContext.UserInRoles
            .AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken);
        if (!exists)
        {
            dbContext.UserInRoles.Add(new UserInRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var memberCount = await dbContext.UserInRoles.CountAsync(x => x.RoleId == role.Id, cancellationToken);
        return RoleMapper.ToDto(role, memberCount);
    }
}

public sealed record RemoveRoleUserCommand(Guid AdminUserId, Guid RoleId, Guid UserId) : ICommand<RoleDto>;

public sealed class RemoveRoleUserCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RemoveRoleUserCommand, RoleDto>
{
    public async Task<RoleDto> Handle(RemoveRoleUserCommand command, CancellationToken cancellationToken)
    {
        await RoleAdminGuard.EnsureAdminAsync(dbContext, command.AdminUserId, cancellationToken);

        var role = await dbContext.Roles
            .Include(x => x.Pages)
            .ThenInclude(x => x.Page)
            .FirstOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var assignment = await dbContext.UserInRoles
            .FirstOrDefaultAsync(x => x.UserId == command.UserId && x.RoleId == role.Id, cancellationToken);
        if (assignment is not null)
        {
            dbContext.UserInRoles.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var memberCount = await dbContext.UserInRoles.CountAsync(x => x.RoleId == role.Id, cancellationToken);
        return RoleMapper.ToDto(role, memberCount);
    }
}
