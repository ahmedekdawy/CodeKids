using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetLessonVideosQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonVideosQuery, IReadOnlyList<LessonVideoDto>>
{
    public async Task<IReadOnlyList<LessonVideoDto>> Handle(
        GetLessonVideosQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.LessonVideos
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Where(x => x.LessonId == query.LessonId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new LessonVideoDto(
                x.Id,
                x.LessonId,
                x.MediaAssetId,
                x.Title,
                x.SortOrder,
                x.MediaAsset!.FileName,
                x.MediaAsset.DurationSeconds))
            .ToListAsync(cancellationToken);
    }
}
