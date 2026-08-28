using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionImages;

public static class QuestionImageAssetValidator
{
    public static async Task EnsureExistsAsync(
        IAppDbContext dbContext,
        Guid? mediaAssetId,
        CancellationToken cancellationToken)
    {
        if (mediaAssetId is not Guid id)
        {
            return;
        }

        var exists = await dbContext.MediaAssets.AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Question image not found.");
        }
    }
}
