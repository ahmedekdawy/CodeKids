using CodeKids.Application.Abstractions;

using CodeKids.Application.Features.SiteSettings;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class SiteSettingsEndpoints

{

    public static IEndpointRouteBuilder MapSiteSettingsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/site-settings", async (

            IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(new GetSiteSettingsQuery(), cancellationToken));

        }).AllowAnonymous();

        app.MapGet("/api/site-settings/logo", async (

            IAppDbContext dbContext,

            IFileStorage fileStorage,

            CancellationToken cancellationToken) =>

        {

            var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);

            if (string.IsNullOrWhiteSpace(settings.LogoStorageKey))

            {

                return Results.NotFound();

            }

            var stream = await fileStorage.OpenReadAsync(settings.LogoStorageKey, cancellationToken);

            return Results.File(stream, string.IsNullOrWhiteSpace(settings.LogoContentType) ? "image/png" : settings.LogoContentType);

        }).AllowAnonymous();

        app.MapGet("/api/site-settings/banner", async (

            IAppDbContext dbContext,

            IFileStorage fileStorage,

            CancellationToken cancellationToken) =>

        {

            var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);

            if (string.IsNullOrWhiteSpace(settings.BannerStorageKey))

            {

                return Results.NotFound();

            }

            var stream = await fileStorage.OpenReadAsync(settings.BannerStorageKey, cancellationToken);

            return Results.File(stream, string.IsNullOrWhiteSpace(settings.BannerContentType) ? "image/jpeg" : settings.BannerContentType);

        }).AllowAnonymous();

        app.MapPut("/api/admin/site-settings", async (

            UpdateSiteSettingsRequest request,

            HttpContext httpContext,

            ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new UpdateSiteSettingsCommand(

                        adminId,

                        request.SiteName,

                        request.ClearLogo == true,

                        request.ClearBanner == true,

                        request.TimetableWeekStartUtc,

                        request.ClearTimetableWeek == true,

                        request.AmSessionCount,

                        request.PmSessionCount,

                        request.PmStartMinutes),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/site-settings/upload", async (

            HttpRequest request,

            HttpContext httpContext,

            IFileStorage fileStorage,

            ICommandHandler<UploadSiteImageCommand, SiteSettingsDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                if (!request.HasFormContentType)

                {

                    return Results.BadRequest(new { code = "api.errors.media.multipartRequired", message = "Expected multipart form upload." });

                }

                var form = await request.ReadFormAsync(cancellationToken);

                var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

                if (file is null || file.Length == 0)

                {

                    return Results.BadRequest(new { code = "api.errors.media.noFile", message = "No file uploaded." });

                }

                var kind = form["kind"].ToString();

                if (string.IsNullOrWhiteSpace(kind))

                {

                    kind = "logo";

                }

                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

                if (file.Length > 5 * 1024 * 1024)

                {

                    return Results.BadRequest(new { message = "Image must be 5 MB or smaller." });

                }

                var adminId = CurrentUser.GetUserId(httpContext.User);

                await using var stream = file.OpenReadStream();

                var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken);

                return Results.Ok(await handler.Handle(

                    new UploadSiteImageCommand(adminId, kind, storageKey, contentType),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" })

            .DisableAntiforgery();

        return app;

    }

}
