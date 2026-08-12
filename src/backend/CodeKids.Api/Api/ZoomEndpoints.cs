using CodeKids.Application.Abstractions;

using CodeKids.Application.Features.ZoomConnect;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ZoomEndpoints

{

    public static IEndpointRouteBuilder MapZoomEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/zoom/status", async (

            HttpContext httpContext,

            IQueryHandler<GetZoomStatusQuery, ZoomConnectionStatus> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetZoomStatusQuery(userId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/zoom/connect", async (

            HttpContext httpContext,

            IQueryHandler<GetZoomConnectUrlQuery, ZoomConnectUrlDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetZoomConnectUrlQuery(userId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/zoom/callback", async (

            string? code,

            string? state,

            string? error,

            ICommandHandler<CompleteZoomConnectCommand, ZoomConnectResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            if (!string.IsNullOrWhiteSpace(error))

            {

                return Results.Redirect($"http://localhost:4200/teacher/zoom?zoom=error&message={Uri.EscapeDataString(error)}");

            }

            try

            {

                var result = await handler.Handle(

                    new CompleteZoomConnectCommand(code ?? string.Empty, state ?? string.Empty),

                    cancellationToken);

                return Results.Redirect(result.FrontendRedirectUrl);

            }

            catch (Exception ex)

            {

                return Results.Redirect(

                    $"http://localhost:4200/teacher/zoom?zoom=error&message={Uri.EscapeDataString(ex.Message)}");

            }

        });

        app.MapPost("/api/zoom/disconnect", async (

            HttpContext httpContext,

            ICommandHandler<DisconnectZoomCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                await handler.Handle(new DisconnectZoomCommand(userId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/zoom/oauth-settings", async (

            IQueryHandler<GetZoomOAuthSettingsQuery, ZoomUserOAuthSettingsDto> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(new GetZoomOAuthSettingsQuery(), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPut("/api/zoom/oauth-settings", async (

            SaveZoomUserOAuthSettingsRequest request,

            ICommandHandler<SaveZoomOAuthSettingsCommand, ZoomUserOAuthSettingsDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new SaveZoomOAuthSettingsCommand(

                        request.ClientId,

                        request.ClientSecret,

                        request.RedirectUri,

                        request.FrontendRedirectUri),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        return app;

    }

}
