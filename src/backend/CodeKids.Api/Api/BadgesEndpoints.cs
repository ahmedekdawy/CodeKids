using CodeKids.Application.Features.Badges;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class BadgesEndpoints

{

    public static IEndpointRouteBuilder MapBadgesEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/badges/me", async (

            HttpContext httpContext,

            IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            return Results.Ok(await handler.Handle(new GetBadgesQuery(userId), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        return app;

    }

}
