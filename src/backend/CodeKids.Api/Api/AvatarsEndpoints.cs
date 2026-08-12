using CodeKids.Application.Features.Avatars;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AvatarsEndpoints

{

    public static IEndpointRouteBuilder MapAvatarsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/avatars", async (

            HttpContext httpContext,

            IQueryHandler<GetAvatarsQuery, IReadOnlyList<AvatarDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            return Results.Ok(await handler.Handle(new GetAvatarsQuery(userId), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapPost("/api/avatars/select", async (

            SelectAvatarRequest request,

            HttpContext httpContext,

            ICommandHandler<SelectAvatarCommand, AvatarDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                var result = await handler.Handle(new SelectAvatarCommand(userId, request.AvatarId), cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        return app;

    }

}
