using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Ai;

public sealed class CourseBookTextProvider(
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    IOptions<MediaOptions> mediaOptions) : ICourseBookTextProvider
{
    public async Task<string> TryGetBookTextAsync(
        Course course,
        int maxCharacters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await dbContext.LearningMaterials
                .AsNoTracking()
                .Include(x => x.MediaAsset)
                .Where(x => x.CourseId == course.Id
                            && x.Kind == LearningMaterialKind.Pdf
                            && x.UnitId == null
                            && x.LessonId == null)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (book?.MediaAsset is null)
            {
                return string.Empty;
            }

            await using var stream = await fileStorage.OpenReadAsync(book.MediaAsset.StorageKey, cancellationToken);
            using var buffered = new MemoryStream();
            await stream.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;

            return PdfTextExtractor.Extract(buffered, maxCharacters: maxCharacters);
        }
        catch
        {
            // The book is a best-effort prompt enhancement; never fail generation over it.
            return string.Empty;
        }
    }
}
