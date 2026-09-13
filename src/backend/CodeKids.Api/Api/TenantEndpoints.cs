using CodeKids.Application.Features.Tenants;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/tenants", async (
            IQueryHandler<ListTenantsQuery, IReadOnlyList<AdminTenantDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(new ListTenantsQuery(), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/tenants/register", async (
            RegisterTenantRequest request,
            ICommandHandler<RegisterTenantCommand, RegisterTenantResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.Handle(
                    new RegisterTenantCommand(
                        request.TenantName,
                        request.Email,
                        request.DisplayName,
                        request.Password,
                        request.MobilePhone),
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        });

        app.MapPost("/api/tenants/verify", async (
            VerifyTenantRequest request,
            ICommandHandler<VerifyTenantCommand, VerifyTenantResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.Handle(new VerifyTenantCommand(request.Token), cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        });

        return app;
    }
}
