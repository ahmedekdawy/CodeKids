using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Application.Features.Media;
using CodeKids.Application.Features.Profile;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class ProfilePhotoEndpoints
{
    public static IEndpointRouteBuilder MapProfilePhotoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profile/photo", async (
            HttpRequest request,
            HttpContext httpContext,
            IFileStorage fileStorage,
            ICommandHandler<SaveProfilePhotoCommand, AuthUserDto> handler,
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

                var contentType = MediaFileTypes.NormalizeContentType(
                    string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType);
                ProfilePhotoUploadRules.EnsureAllowed(contentType, file.Length);
                var uploadFileName = MediaFileTypes.EnsureFileName(file.FileName, contentType);

                var userId = CurrentUser.GetUserId(httpContext.User);
                await using var stream = file.OpenReadStream();
                var storageKey = await fileStorage.SaveAsync(stream, uploadFileName, contentType, cancellationToken);

                return Results.Ok(await handler.Handle(
                    new SaveProfilePhotoCommand(userId, storageKey, contentType),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization()
            .DisableAntiforgery();

        app.MapDelete("/api/profile/photo", async (
            HttpContext httpContext,
            ICommandHandler<RemoveProfilePhotoCommand, AuthUserDto> handler,
            CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.GetUserId(httpContext.User);
            return Results.Ok(await handler.Handle(new RemoveProfilePhotoCommand(userId), cancellationToken));
        }).RequireAuthorization();

        app.MapGet("/api/users/{userId:guid}/photo", async (
            Guid userId,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var photo = await dbContext.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.ProfilePhotoStorageKey, x.ProfilePhotoContentType })
                .FirstOrDefaultAsync(cancellationToken);
            if (photo is null || string.IsNullOrWhiteSpace(photo.ProfilePhotoStorageKey))
            {
                return Results.NotFound();
            }

            var stream = await fileStorage.OpenReadAsync(photo.ProfilePhotoStorageKey, cancellationToken);
            var contentType = MediaFileTypes.ResolveContentType(
                photo.ProfilePhotoContentType,
                photo.ProfilePhotoStorageKey);
            return Results.File(stream, contentType);
        }).RequireAuthorization();

        return app;
    }
}
