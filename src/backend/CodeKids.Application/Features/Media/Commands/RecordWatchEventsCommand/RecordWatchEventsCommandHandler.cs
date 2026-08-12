using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class RecordWatchEventsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RecordWatchEventsCommand, WatchSessionDto>
{
    public async Task<WatchSessionDto> Handle(RecordWatchEventsCommand command, CancellationToken cancellationToken)
    {
        _ = await dbContext.MediaAssets.AnyAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ? true
            : throw new InvalidOperationException("Media asset not found.");

        VideoWatchSession? session = null;
        if (command.SessionId is Guid sessionId)
        {
            session = await dbContext.VideoWatchSessions
                .FirstOrDefaultAsync(
                    x => x.Id == sessionId && x.StudentId == command.StudentId,
                    cancellationToken);
        }

        session ??= await dbContext.VideoWatchSessions
            .Where(x => x.MediaAssetId == command.MediaAssetId && x.StudentId == command.StudentId)
            .OrderByDescending(x => x.LastEventAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            session = new VideoWatchSession
            {
                Id = Guid.NewGuid(),
                MediaAssetId = command.MediaAssetId,
                StudentId = command.StudentId,
                LessonId = command.LessonId,
                StartedAtUtc = DateTimeOffset.UtcNow,
                LastEventAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.VideoWatchSessions.Add(session);
        }

        var lastPos = session.MaxPositionSeconds;
        foreach (var evt in command.Events.OrderBy(e => e.ClientAtUtc ?? DateTimeOffset.UtcNow))
        {
            var type = (evt.EventType ?? string.Empty).Trim().ToLowerInvariant();
            session.LastEventAtUtc = evt.ClientAtUtc ?? DateTimeOffset.UtcNow;
            session.MaxPositionSeconds = Math.Max(session.MaxPositionSeconds, Math.Max(0, evt.PositionSeconds));

            if (type is "heartbeat" or "play" or "pause" or "ended")
            {
                // Heartbeats every ~5s; credit up to 8s of real watch time per event.
                session.ActualWatchSeconds += type == "heartbeat" ? 5 : 1;
            }

            if (evt.PlaybackRate is > 1.05)
            {
                session.UsedSpeedUp = true;
            }

            if (type == "seek"
                && evt.FromSeconds is int from
                && evt.ToSeconds is int to
                && to - from > 8)
            {
                session.SkippedAhead = true;
            }
            else if (type == "seek" && evt.PositionSeconds - lastPos > 8)
            {
                session.SkippedAhead = true;
            }

            lastPos = evt.PositionSeconds;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var student = await dbContext.Users.AsNoTracking()
            .FirstAsync(x => x.Id == command.StudentId, cancellationToken);

        return new WatchSessionDto(
            session.Id,
            session.MediaAssetId,
            session.StudentId,
            student.DisplayName,
            session.LessonId,
            session.ActualWatchSeconds,
            session.MaxPositionSeconds,
            session.UsedSpeedUp,
            session.SkippedAhead,
            session.StartedAtUtc,
            session.LastEventAtUtc);
    }
}
