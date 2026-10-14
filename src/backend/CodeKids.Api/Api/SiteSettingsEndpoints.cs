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

            string? tenant,

            IAppDbContext dbContext,

            IFileStorage fileStorage,

            CancellationToken cancellationToken) =>

        {

            var image = await LoadImageAsync(dbContext, tenant, x => new SiteImage(x.LogoStorageKey, x.LogoContentType), cancellationToken);

            if (image is null || string.IsNullOrWhiteSpace(image.StorageKey))

            {

                return Results.NotFound();

            }

            var stream = await fileStorage.OpenReadAsync(image.StorageKey, cancellationToken);

            return Results.File(stream, string.IsNullOrWhiteSpace(image.ContentType) ? "image/png" : image.ContentType);

        }).AllowAnonymous();

        app.MapGet("/api/site-settings/banner", async (

            string? tenant,

            IAppDbContext dbContext,

            IFileStorage fileStorage,

            CancellationToken cancellationToken) =>

        {

            var image = await LoadImageAsync(dbContext, tenant, x => new SiteImage(x.BannerStorageKey, x.BannerContentType), cancellationToken);

            if (image is null || string.IsNullOrWhiteSpace(image.StorageKey))

            {

                return Results.NotFound();

            }

            var stream = await fileStorage.OpenReadAsync(image.StorageKey, cancellationToken);

            return Results.File(stream, string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType);

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

                        request.PmStartMinutes,

                        request.MultiTimeZone,

                        request.TimeZone),

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

            CodeKids.Infrastructure.Tenancy.ITenantContext tenantContext,

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

                // Some phone file pickers send no type (or a generic one); fall back to the file's extension.
                var contentType = CodeKids.Application.Features.Media.MediaFileTypes.ResolveContentType(
                    string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
                    file.FileName);

                if (file.Length > 5 * 1024 * 1024)

                {

                    return Results.BadRequest(new { message = "Image must be 5 MB or smaller." });

                }

                var adminId = CurrentUser.GetUserId(httpContext.User);

                await using var stream = file.OpenReadStream();

                var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken, tenantContext.TenantId);

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

    private sealed record SiteImage(string StorageKey, string ContentType);

    /// <summary>
    /// The named tenant's image when the URL carries one; otherwise the request's tenant, as before.
    /// Read-only: an image request must not create settings rows for arbitrary tenant names.
    /// </summary>
    private static async Task<SiteImage?> LoadImageAsync(
        IAppDbContext dbContext,
        string? tenant,
        System.Linq.Expressions.Expression<Func<CodeKids.Domain.Entities.SiteSettings, SiteImage>> select,
        CancellationToken cancellationToken)
    {
        var tenantId = (tenant ?? string.Empty).Trim().ToLowerInvariant();
        if (tenantId.Length is > 0 and <= 64 && tenantId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(dbContext.SiteSettings)
                    .Where(x => x.TenantId == tenantId)
                    .Select(select),
                cancellationToken);
        }

        var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);
        return select.Compile()(settings);
    }

}
