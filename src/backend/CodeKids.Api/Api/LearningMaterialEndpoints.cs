using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.LearningMaterials;
using CodeKids.Application.Features.Media;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class LearningMaterialEndpoints
{
    public static IEndpointRouteBuilder MapLearningMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/media/learning-materials/upload", async (
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
                var contentType = MediaFileTypes.ResolveContentType(
                    string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
                    file.FileName);
                LearningMaterialUploadRules.EnsureAllowed(contentType, file.Length, mediaOptions.Value.MaxUploadBytes);
                var uploadFileName = MediaFileTypes.EnsureFileName(file.FileName, contentType);

                var userId = CurrentUser.GetUserId(httpContext.User);
                await using var stream = file.OpenReadStream();
                var storageKey = await fileStorage.SaveAsync(stream, uploadFileName, contentType, cancellationToken);
                var asset = new CodeKids.Domain.Entities.MediaAsset
                {
                    Id = Guid.NewGuid(),
                    StorageKey = storageKey,
                    FileName = uploadFileName,
                    ContentType = contentType,
                    SizeBytes = file.Length,
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

        app.MapPost("/api/learning-materials", async (
            AttachLearningMaterialRequest request,
            HttpContext httpContext,
            ICommandHandler<AttachLearningMaterialCommand, LearningMaterialDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                return Results.Ok(await handler.Handle(
                    new AttachLearningMaterialCommand(
                        userId,
                        role,
                        request.CourseId,
                        request.UnitId,
                        request.LessonId,
                        request.MediaAssetId,
                        request.Title,
                        request.SortOrder ?? 1),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/learning-materials", async (
            HttpContext httpContext,
            IQueryHandler<GetTeacherLearningMaterialsQuery, IReadOnlyList<TeacherLearningMaterialDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.GetUserId(httpContext.User);
            var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? httpContext.User.FindFirst("role")?.Value;
            return Results.Ok(await handler.Handle(new GetTeacherLearningMaterialsQuery(userId, role), cancellationToken));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/learning-materials/page", async (
            Guid? courseId,
            Guid? unitId,
            Guid? lessonId,
            bool? all,
            HttpContext httpContext,
            IQueryHandler<GetLearningMaterialPageQuery, LearningMaterialPageDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                return Results.Ok(await handler.Handle(
                    new GetLearningMaterialPageQuery(userId, role, courseId, unitId, lessonId, all ?? false),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        app.MapDelete("/api/learning-materials/{materialId:guid}", async (
            Guid materialId,
            HttpContext httpContext,
            ICommandHandler<DeleteLearningMaterialCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
                await handler.Handle(new DeleteLearningMaterialCommand(userId, role, materialId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/learning-materials/{materialId:guid}/file", async (
            Guid materialId,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var material = await dbContext.LearningMaterials.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == materialId, cancellationToken);
            if (material is null)
            {
                return Results.NotFound();
            }

            var media = await dbContext.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == material.MediaAssetId, cancellationToken);
            if (media is null)
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrWhiteSpace(media.ExternalUrl))
            {
                return Results.Redirect(media.ExternalUrl);
            }

            if (string.IsNullOrWhiteSpace(media.StorageKey))
            {
                return Results.NotFound();
            }

            var stream = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
            var contentType = MediaFileTypes.ResolveContentType(media.ContentType, media.StorageKey);
            return Results.File(stream, contentType, enableRangeProcessing: true);
        }).RequireAuthorization();

        return app;
    }
}

public sealed record AttachLearningMaterialRequest(
    Guid? CourseId,
    Guid? UnitId,
    Guid? LessonId,
    Guid MediaAssetId,
    string? Title,
    int? SortOrder);
