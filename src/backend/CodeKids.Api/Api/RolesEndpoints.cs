using CodeKids.Application.Features.Roles;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var adminOnly = new AuthorizeAttribute { Roles = "SuperAdmin" };

        app.MapGet("/api/admin/pages", async (
            IQueryHandler<ListPagesQuery, IReadOnlyList<PageDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListPagesQuery(), cancellationToken));
        }).RequireAuthorization(adminOnly);

        app.MapGet("/api/admin/roles", async (
            string? baseRole,
            IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListRolesQuery(baseRole), cancellationToken));
        }).RequireAuthorization(adminOnly);

        app.MapGet("/api/admin/roles/{roleId:guid}/members", async (
            Guid roleId,
            IQueryHandler<ListRoleMembersQuery, IReadOnlyList<RoleMemberDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListRoleMembersQuery(roleId), cancellationToken));
        }).RequireAuthorization(adminOnly);

        app.MapPost("/api/admin/roles", async (
            CreateRoleRequest request,
            HttpContext httpContext,
            ICommandHandler<CreateRoleCommand, RoleDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new CreateRoleCommand(
                        adminId,
                        request.Name,
                        request.DisplayName,
                        request.Description,
                        request.BaseRole,
                        request.PageKeys),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(adminOnly);

        app.MapPut("/api/admin/roles/{roleId:guid}", async (
            Guid roleId,
            UpdateRoleRequest request,
            HttpContext httpContext,
            ICommandHandler<UpdateRoleCommand, RoleDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new UpdateRoleCommand(
                        adminId,
                        roleId,
                        request.DisplayName,
                        request.Description,
                        request.PageKeys),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(adminOnly);

        app.MapDelete("/api/admin/roles/{roleId:guid}", async (
            Guid roleId,
            HttpContext httpContext,
            ICommandHandler<DeleteRoleCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteRoleCommand(adminId, roleId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(adminOnly);

        app.MapPost("/api/admin/roles/{roleId:guid}/users", async (
            Guid roleId,
            AssignRoleUserRequest request,
            HttpContext httpContext,
            ICommandHandler<AssignRoleUserCommand, RoleDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new AssignRoleUserCommand(adminId, roleId, request.UserId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(adminOnly);

        app.MapDelete("/api/admin/roles/{roleId:guid}/users/{userId:guid}", async (
            Guid roleId,
            Guid userId,
            HttpContext httpContext,
            ICommandHandler<RemoveRoleUserCommand, RoleDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new RemoveRoleUserCommand(adminId, roleId, userId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(adminOnly);

        return app;
    }

    public sealed record AssignRoleUserRequest(Guid UserId);
}
