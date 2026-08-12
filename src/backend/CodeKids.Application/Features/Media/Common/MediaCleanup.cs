using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

internal static class MediaCleanup
{
    public static async Task TryDeleteOrphanMediaAsync(
        IAppDbContext dbContext,
        IFileStorage fileStorage,
        Guid mediaId,
        string? storageKey,
        CancellationToken cancellationToken)
    {
        var stillUsed =
            await dbContext.LessonVideos.AnyAsync(x => x.MediaAssetId == mediaId, cancellationToken)
            || await dbContext.Assignments.AnyAsync(x => x.SolutionVideoMediaAssetId == mediaId, cancellationToken);

        if (stillUsed)
        {
            return;
        }

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == mediaId, cancellationToken);
        if (media is null)
        {
            return;
        }

        var key = storageKey ?? media.StorageKey;
        dbContext.MediaAssets.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(key))
        {
            await fileStorage.DeleteAsync(key, cancellationToken);
        }
    }
}
