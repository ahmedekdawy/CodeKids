using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
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
                var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken);
                var asset = new CodeKids.Domain.Entities.MediaAsset
                {
                    Id = Guid.NewGuid(),
                    StorageKey = storageKey,
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

        app.MapGet("/api/media/{mediaAssetId:guid}/playback", async (
            Guid mediaAssetId,
            HttpContext httpContext,
            IQueryHandler<GetPlaybackQuery, PlaybackDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var baseApiUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api";
                return Results.Ok(await handler.Handle(
                    new GetPlaybackQuery(mediaAssetId, userId, baseApiUrl),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        app.MapGet("/api/media/stream", async (
            string token,
            IMediaAccessTokenService tokenService,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            if (!tokenService.TryValidate(token, out var mediaAssetId, out _, out _))
            {
                return Results.Unauthorized();
            }
            var media = await dbContext.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
            if (media is null || string.IsNullOrWhiteSpace(media.StorageKey))
            {
                return Results.NotFound();
            }
            var stream = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
            return Results.File(
                stream,
                contentType: media.ContentType,
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
}
