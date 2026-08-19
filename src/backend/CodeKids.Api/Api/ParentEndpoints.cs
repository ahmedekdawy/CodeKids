using CodeKids.Application.Features.Dashboard;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ParentEndpoints
{
    public static IEndpointRouteBuilder MapParentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/parent/children/{childId:guid}", async (
            Guid childId,
            HttpContext httpContext,
            IQueryHandler<GetParentChildOverviewQuery, ParentChildOverviewDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetParentChildOverviewQuery(userId, childId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        return app;
    }
}
