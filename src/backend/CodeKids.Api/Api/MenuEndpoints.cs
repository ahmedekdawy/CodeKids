using CodeKids.Application.Features.Menu;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;

namespace CodeKids.Api;

public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu", async (
            HttpContext httpContext,
            IQueryHandler<GetMyMenuQuery, MyMenuDto> handler,
            CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.GetUserId(httpContext.User);
            return Results.Ok(await handler.Handle(new GetMyMenuQuery(userId), cancellationToken));
        }).RequireAuthorization();

        app.MapPut("/api/menu", async (
            SaveMyMenuRequest request,
            HttpContext httpContext,
            ICommandHandler<SaveMyMenuCommand, MyMenuDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new SaveMyMenuCommand(userId, request.Items ?? []),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        app.MapPost("/api/menu/reset", async (
            HttpContext httpContext,
            ICommandHandler<ResetMyMenuCommand, MyMenuDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new ResetMyMenuCommand(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        return app;
    }
}
