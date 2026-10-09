using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapPost("/api/media/upload", async (
            HttpRequest request,
            HttpContext httpContext,
            IFileStorage fileStorage,
            IAppDbContext dbContext,
            ITenantContext tenantContext,
            Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions,
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
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
                MediaUploadRules.EnsureAllowed(contentType, file.Length, mediaOptions.Value.MaxUploadBytes);
                int? durationSeconds = null;
                if (int.TryParse(form["durationSeconds"], out var parsedDuration) && parsedDuration > 0)
                {
                    durationSeconds = parsedDuration;
                }
                var userId = CurrentUser.GetUserId(httpContext.User);
                await using var stream = file.OpenReadStream();
                var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken, tenantContext.TenantId);
                var asset = new CodeKids.Domain.Entities.MediaAsset
                {
                    Id = Guid.NewGuid(),
                    StorageKey = storageKey,
                    FilePath = fileStorage.GetRelativePath(storageKey),
                    FileName = Path.GetFileName(file.FileName),
                    ContentType = contentType,
                    SizeBytes = file.Length,
                    DurationSeconds = durationSeconds,
                    UploadedByUserId = userId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                dbContext.MediaAssets.Add(asset);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(new MediaAssetDto(
                    asset.Id,
                    asset.FileName,
                    asset.ContentType,
                    asset.SizeBytes,
                    asset.DurationSeconds,
                    asset.CreatedAtUtc));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" })
            .DisableAntiforgery();


        app.MapPost("/api/media/from-url", async (
            RegisterMediaFromUrlRequest request,
            HttpContext httpContext,
            ICommandHandler<RegisterMediaFromUrlCommand, MediaAssetDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new RegisterMediaFromUrlCommand(userId, request.Url, request.Title),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/lessons/{lessonId:guid}/videos", async (
            Guid lessonId,
            AttachLessonVideoRequest request,
            HttpContext httpContext,
            ICommandHandler<AttachLessonVideoCommand, LessonVideoDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new AttachLessonVideoCommand(userId, lessonId, request.MediaAssetId, request.Title, request.SortOrder),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/lessons/{lessonId:guid}/videos", async (
            Guid lessonId,
            IQueryHandler<GetLessonVideosQuery, IReadOnlyList<LessonVideoDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new GetLessonVideosQuery(lessonId), cancellationToken));
        }).RequireAuthorization();

        app.MapGet("/api/media/library", async (
            HttpContext httpContext,
            IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto> handler,
            CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.GetUserId(httpContext.User);
            return Results.Ok(await handler.Handle(new GetTeacherVideoLibraryQuery(userId), cancellationToken));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapDelete("/api/lessons/videos/{lessonVideoId:guid}", async (
            Guid lessonVideoId,
            HttpContext httpContext,
            ICommandHandler<DeleteLessonVideoCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteLessonVideoCommand(userId, lessonVideoId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/courses/{courseId:guid}/videos", async (
            Guid courseId,
            AttachLessonVideoRequest request,
            HttpContext httpContext,
            ICommandHandler<AttachLessonVideoCommand, LessonVideoDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new AttachLessonVideoCommand(
                        userId,
                        LessonId: null,
                        request.MediaAssetId,
                        request.Title,
                        request.SortOrder,
                        courseId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/media/course-videos", async (
            HttpContext httpContext,
            IQueryHandler<GetCourseVideoLibraryQuery, IReadOnlyList<CourseVideoLibraryItemDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new GetCourseVideoLibraryQuery(userId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/courses/videos/{courseVideoId:guid}", async (
            Guid courseVideoId,
            HttpContext httpContext,
            ICommandHandler<DeleteLessonVideoCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteLessonVideoCommand(userId, courseVideoId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/media/{mediaAssetId:guid}/playback", async (
            Guid mediaAssetId,
            string? apiBase,
            HttpContext httpContext,
            TenantCatalog tenantCatalog,
            Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions,
            IQueryHandler<GetPlaybackQuery, PlaybackDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var tenant = tenantCatalog.Resolve(
                    httpContext.Request.Headers[TenantCatalog.HeaderName].ToString(),
                    httpContext.Request.Headers.Origin.ToString(),
                    httpContext.Request.Host.Host);
                var baseApiUrl = MediaApiBaseUrl.Resolve(
                    apiBase,
                    tenant,
                    mediaOptions.Value.PublicBaseUrl,
                    httpContext);
                return Results.Ok(await handler.Handle(
                    new GetPlaybackQuery(mediaAssetId, userId, baseApiUrl, tenant.Id),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        app.MapGet("/api/media/stream", async (
            string token,
            HttpContext httpContext,
            IMediaAccessTokenService tokenService,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            TenantCatalog tenantCatalog,
            Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext> dbOptions,
            CancellationToken cancellationToken) =>
        {
            if (!tokenService.TryValidate(token, out var mediaAssetId, out _, out _, out var tokenTenantId))
            {
                return Results.Unauthorized();
            }

            // Anonymous <video> requests carry no X-Tenant-Id header, so the scoped
            // DbContext would resolve to the default tenant. The signed token embeds
            // the tenant that issued the playback URL - use it to scope the lookup.
            var media = await LoadMediaForTokenTenantAsync(
                dbContext,
                dbOptions,
                tenantCatalog,
                tokenTenantId,
                httpContext,
                mediaAssetId,
                cancellationToken);
            if (media is null)
            if (media is null || string.IsNullOrWhiteSpace(media.StorageKey))
            {
                return Results.NotFound();
            }
            var contentType = MediaFileTypes.ResolveContentType(media.ContentType, media.FileName);

            // Serve proper 206 Partial Content responses so the browser's <video> element
            // can seek/scrub forward and backward within streamed videos.
            var rangeHeader = httpContext.Request.Headers.Range.ToString();
            if (!string.IsNullOrWhiteSpace(rangeHeader) &&
                System.Net.Http.Headers.RangeHeaderValue.TryParse(rangeHeader, out var range) &&
                range.Ranges.Count > 0)
            {
                var spec = range.Ranges.First();
                long? start = spec.From;
                long? end = spec.To;

                if (!start.HasValue && end.HasValue)
                {
                    // Suffix range (bytes=-N): last N bytes; requires total length.
                    var probe = await fileStorage.OpenRangeAsync(media.StorageKey, null, null, cancellationToken);
                    if (probe.TotalLength is long totalForSuffix)
                    {
                        await probe.Content.DisposeAsync();
                        start = Math.Max(0, totalForSuffix - end.Value);
                        end = totalForSuffix - 1;
                    }
                    else
                    {
                        await probe.Content.DisposeAsync();
                        start = 0;
                    }
                }

                var result = await fileStorage.OpenRangeAsync(media.StorageKey, start, end, cancellationToken);
                if (!result.RangeHandled)
                {
                    await result.Content.DisposeAsync();
                    // Provider cannot serve ranges; fall back to full-content streaming.
                    var fallback = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
                    return Results.File(fallback, contentType, enableRangeProcessing: true);
                }

                if (result.TotalLength.HasValue && start.HasValue && start.Value >= result.TotalLength.Value)
                {
                    await result.Content.DisposeAsync();
                    return Results.StatusCode(StatusCodes.Status416RangeNotSatisfiable);
                }

                long from = result.Start ?? start ?? 0;
                long to = result.End ?? (result.TotalLength.HasValue ? result.TotalLength.Value - 1 : from);
                long totalLength = result.TotalLength ?? to + 1;

                httpContext.Response.StatusCode = StatusCodes.Status206PartialContent;
                httpContext.Response.ContentType = contentType;
                httpContext.Response.Headers.AcceptRanges = "bytes";
                httpContext.Response.Headers.ContentRange = $"bytes {from}-{to}/{totalLength}";
                httpContext.Response.ContentLength = to - from + 1;
                await result.Content.CopyToAsync(httpContext.Response.Body, cancellationToken);
                return Results.Empty;
            }

            // No Range header: full download; still advertise range support for future seeks.
            var stream = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
            httpContext.Response.Headers.AcceptRanges = "bytes";
            return Results.File(
                stream,
                contentType: contentType,
                fileDownloadName: null,
                enableRangeProcessing: true);
        }).AllowAnonymous();

        app.MapPost("/api/media/watch-events", async (
            RecordWatchEventsRequest request,
            HttpContext httpContext,
            ICommandHandler<RecordWatchEventsCommand, WatchSessionDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new RecordWatchEventsCommand(
                        userId,
                        request.MediaAssetId,
                        request.LessonId,
                        request.SessionId,
                        request.Events),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/media/{mediaAssetId:guid}/watch-sessions", async (
            Guid mediaAssetId,
            HttpContext httpContext,
            IQueryHandler<GetWatchSessionsQuery, IReadOnlyList<WatchSessionDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.GetUserId(httpContext.User);
            return Results.Ok(await handler.Handle(new GetWatchSessionsQuery(userId, mediaAssetId), cancellationToken));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });
        return app;
    }

    private static async Task<MediaAsset?> LoadMediaForTokenTenantAsync(
        IAppDbContext dbContext,
        Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext> dbOptions,
        TenantCatalog tenantCatalog,
        string? tokenTenantId,
        HttpContext httpContext,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        // All tenants use the same connection string � always use the provided request-scoped dbContext.
        // Ensure we bypass any global query filters (tenant scoping) and explicitly filter by the token tenant id.
        return await dbContext.MediaAssets
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == mediaAssetId &&
                (tokenTenantId == null || x.TenantId == tokenTenantId),
                cancellationToken);
    }
}
