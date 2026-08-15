using CodeKids.Application.Features.Tenants;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Api;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
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
                        request.Password),
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
