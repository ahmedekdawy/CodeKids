using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class QuestionImageEndpoints
{
    public static IEndpointRouteBuilder MapQuestionImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/question-images/upload", async (
            HttpRequest request,
            HttpContext httpContext,
            IFileStorage fileStorage,
            IAppDbContext dbContext,
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
                QuestionImageUploadRules.EnsureAllowed(contentType, file.Length);

                var userId = CurrentUser.GetUserId(httpContext.User);
                await using var stream = file.OpenReadStream();
                var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken);
                var asset = new MediaAsset
                {
                    Id = Guid.NewGuid(),
                    StorageKey = storageKey,
                    FileName = Path.GetFileName(file.FileName),
                    ContentType = contentType,
                    SizeBytes = file.Length,
                    UploadedByUserId = userId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                dbContext.MediaAssets.Add(asset);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(new QuestionImageUploadDto(
                    asset.Id,
                    QuestionImageUrls.Build(asset.Id)!,
                    asset.ContentType));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" })
            .DisableAntiforgery();

        app.MapGet("/api/question-images/{mediaAssetId:guid}", async (
            Guid mediaAssetId,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var media = await dbContext.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
            if (media is null || string.IsNullOrWhiteSpace(media.StorageKey))
            {
                return Results.NotFound();
            }

            var stream = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
            return Results.File(stream, media.ContentType);
        }).RequireAuthorization();

        return app;
    }
}
