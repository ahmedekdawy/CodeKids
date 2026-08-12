using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetWatchSessionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetWatchSessionsQuery, IReadOnlyList<WatchSessionDto>>
{
    public async Task<IReadOnlyList<WatchSessionDto>> Handle(
        GetWatchSessionsQuery query,
        CancellationToken cancellationToken)
    {
        _ = query.TeacherUserId;
        return await dbContext.VideoWatchSessions
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.MediaAssetId == query.MediaAssetId)
            .OrderByDescending(x => x.LastEventAtUtc)
            .Select(x => new WatchSessionDto(
                x.Id,
                x.MediaAssetId,
                x.StudentId,
                x.Student!.DisplayName,
                x.LessonId,
                x.ActualWatchSeconds,
                x.MaxPositionSeconds,
                x.UsedSpeedUp,
                x.SkippedAhead,
                x.StartedAtUtc,
                x.LastEventAtUtc))
            .ToListAsync(cancellationToken);
    }
}
