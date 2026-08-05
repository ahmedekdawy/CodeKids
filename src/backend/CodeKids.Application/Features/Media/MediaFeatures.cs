using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record AttachLessonVideoRequest(Guid MediaAssetId, string? Title = null, int SortOrder = 1);

public sealed record MediaAssetDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int? DurationSeconds,
    DateTimeOffset CreatedAtUtc);

public sealed record LessonVideoDto(
    Guid Id,
    Guid LessonId,
    Guid MediaAssetId,
    string Title,
    int SortOrder,
    string FileName,
    int? DurationSeconds);

public sealed record PlaybackDto(
    Guid MediaAssetId,
    string PlaybackUrl,
    string WatermarkText,
    DateTimeOffset ExpiresAtUtc,
    int? DurationSeconds,
    string ContentType,
    string FileName);

public sealed record WatchEventInput(
    string EventType,
    int PositionSeconds,
    double? PlaybackRate,
    int? FromSeconds,
    int? ToSeconds,
    DateTimeOffset? ClientAtUtc);

public sealed record RecordWatchEventsRequest(
    Guid MediaAssetId,
    Guid? LessonId,
    Guid? SessionId,
    IReadOnlyList<WatchEventInput> Events);

public sealed record WatchSessionDto(
    Guid Id,
    Guid MediaAssetId,
    Guid StudentId,
    string StudentName,
    Guid? LessonId,
    int ActualWatchSeconds,
    int MaxPositionSeconds,
    bool UsedSpeedUp,
    bool SkippedAhead,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastEventAtUtc);

public sealed record AttachLessonVideoCommand(
    Guid TeacherUserId,
    Guid LessonId,
    Guid MediaAssetId,
    string? Title,
    int SortOrder) : ICommand<LessonVideoDto>;

public sealed record AttachAssignmentSolutionVideoCommand(
    Guid TeacherUserId,
    Guid AssignmentId,
    Guid MediaAssetId) : ICommand<MediaAssetDto>;

public sealed record GetLessonVideosQuery(Guid LessonId) : IQuery<IReadOnlyList<LessonVideoDto>>;

public sealed record GetPlaybackQuery(Guid MediaAssetId, Guid UserId, string BaseApiUrl) : IQuery<PlaybackDto>;

public sealed record RecordWatchEventsCommand(
    Guid StudentId,
    Guid MediaAssetId,
    Guid? LessonId,
    Guid? SessionId,
    IReadOnlyList<WatchEventInput> Events) : ICommand<WatchSessionDto>;

public sealed record GetWatchSessionsQuery(Guid TeacherUserId, Guid MediaAssetId)
    : IQuery<IReadOnlyList<WatchSessionDto>>;

public sealed record TeacherLessonVideoDto(
    Guid Id,
    Guid LessonId,
    string LessonTitle,
    Guid CourseId,
    string CourseTitle,
    Guid MediaAssetId,
    string Title,
    string FileName,
    long SizeBytes,
    int? DurationSeconds,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);

public sealed record TeacherSolutionVideoDto(
    Guid AssignmentId,
    string AssignmentTitle,
    Guid ClassroomId,
    string ClassroomName,
    Guid MediaAssetId,
    string FileName,
    long SizeBytes,
    int? DurationSeconds,
    DateTimeOffset CreatedAtUtc);

public sealed record TeacherVideoLibraryDto(
    IReadOnlyList<TeacherLessonVideoDto> LessonVideos,
    IReadOnlyList<TeacherSolutionVideoDto> SolutionVideos);

public sealed record GetTeacherVideoLibraryQuery(Guid TeacherUserId) : IQuery<TeacherVideoLibraryDto>;

public sealed record DeleteLessonVideoCommand(Guid TeacherUserId, Guid LessonVideoId) : ICommand<bool>;

public sealed record DeleteAssignmentSolutionVideoCommand(Guid TeacherUserId, Guid AssignmentId) : ICommand<bool>;

public static class MediaUploadRules
{
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/quicktime"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes, long maxBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only MP4, WebM, and MOV videos are allowed.");
        }

        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new InvalidOperationException($"File size must be between 1 byte and {maxBytes} bytes.");
        }
    }
}

public sealed class AttachLessonVideoCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AttachLessonVideoCommand, LessonVideoDto>
{
    public async Task<LessonVideoDto> Handle(AttachLessonVideoCommand command, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == command.LessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        if (media.UploadedByUserId != command.TeacherUserId)
        {
            var isAdmin = await dbContext.Users.AnyAsync(
                x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
                cancellationToken);
            if (!isAdmin)
            {
                throw new InvalidOperationException("You can only attach media you uploaded.");
            }
        }

        var video = new LessonVideo
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            MediaAssetId = media.Id,
            Title = string.IsNullOrWhiteSpace(command.Title) ? media.FileName : command.Title.Trim(),
            SortOrder = command.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LessonVideos.Add(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LessonVideoDto(
            video.Id,
            video.LessonId,
            video.MediaAssetId,
            video.Title,
            video.SortOrder,
            media.FileName,
            media.DurationSeconds);
    }
}

public sealed class AttachAssignmentSolutionVideoCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto>
{
    public async Task<MediaAssetDto> Handle(
        AttachAssignmentSolutionVideoCommand command,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.TeacherId != command.TeacherUserId
            && assignment.CreatedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("Only the classroom teacher can attach a solution video.");
        }

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        assignment.SolutionVideoMediaAssetId = media.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaAssetDto(
            media.Id,
            media.FileName,
            media.ContentType,
            media.SizeBytes,
            media.DurationSeconds,
            media.CreatedAtUtc);
    }
}

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

public sealed class GetPlaybackQueryHandler(
    IAppDbContext dbContext,
    IMediaAccessTokenService tokenService,
    Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions)
    : IQueryHandler<GetPlaybackQuery, PlaybackDto>
{
    public async Task<PlaybackDto> Handle(GetPlaybackQuery query, CancellationToken cancellationToken)
    {
        var media = await dbContext.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var lifetime = TimeSpan.FromMinutes(Math.Clamp(mediaOptions.Value.SignedUrlMinutes, 1, 120));
        var token = tokenService.CreateToken(media.Id, user.Id, lifetime);
        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var baseUrl = query.BaseApiUrl.TrimEnd('/');
        var playbackUrl = $"{baseUrl}/media/stream?token={Uri.EscapeDataString(token)}";

        return new PlaybackDto(
            media.Id,
            playbackUrl,
            $"{user.DisplayName} · {user.Email}",
            expires,
            media.DurationSeconds,
            media.ContentType,
            media.FileName);
    }
}

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

public sealed class GetTeacherVideoLibraryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto>
{
    public async Task<TeacherVideoLibraryDto> Handle(
        GetTeacherVideoLibraryQuery query,
        CancellationToken cancellationToken)
    {
        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == query.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);

        var lessonVideos = await dbContext.LessonVideos
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Include(x => x.Lesson)!.ThenInclude(l => l!.Course)
            .Where(x => isAdmin || x.MediaAsset!.UploadedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TeacherLessonVideoDto(
                x.Id,
                x.LessonId,
                x.Lesson!.Title,
                x.Lesson.CourseId,
                x.Lesson.Course!.Title,
                x.MediaAssetId,
                x.Title,
                x.MediaAsset!.FileName,
                x.MediaAsset.SizeBytes,
                x.MediaAsset.DurationSeconds,
                x.SortOrder,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var solutionVideos = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom)
            .Include(x => x.SolutionVideo)
            .Where(x => x.SolutionVideoMediaAssetId != null)
            .Where(x => isAdmin
                || x.CreatedByUserId == query.TeacherUserId
                || x.Classroom!.TeacherId == query.TeacherUserId
                || x.SolutionVideo!.UploadedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.SolutionVideo!.CreatedAtUtc)
            .Select(x => new TeacherSolutionVideoDto(
                x.Id,
                x.Title,
                x.ClassroomId,
                x.Classroom!.Name,
                x.SolutionVideoMediaAssetId!.Value,
                x.SolutionVideo!.FileName,
                x.SolutionVideo.SizeBytes,
                x.SolutionVideo.DurationSeconds,
                x.SolutionVideo.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new TeacherVideoLibraryDto(lessonVideos, solutionVideos);
    }
}

public sealed class DeleteLessonVideoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<DeleteLessonVideoCommand, bool>
{
    public async Task<bool> Handle(DeleteLessonVideoCommand command, CancellationToken cancellationToken)
    {
        var video = await dbContext.LessonVideos
            .Include(x => x.MediaAsset)
            .FirstOrDefaultAsync(x => x.Id == command.LessonVideoId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson video not found.");

        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin && video.MediaAsset?.UploadedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("You can only delete videos you uploaded.");
        }

        var mediaId = video.MediaAssetId;
        var storageKey = video.MediaAsset?.StorageKey;

        dbContext.LessonVideos.Remove(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        await MediaCleanup.TryDeleteOrphanMediaAsync(dbContext, fileStorage, mediaId, storageKey, cancellationToken);
        return true;
    }
}

public sealed class DeleteAssignmentSolutionVideoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool>
{
    public async Task<bool> Handle(DeleteAssignmentSolutionVideoCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom)
            .Include(x => x.SolutionVideo)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.SolutionVideoMediaAssetId is null)
        {
            throw new InvalidOperationException("Assignment has no solution video.");
        }

        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin
            && assignment.Classroom?.TeacherId != command.TeacherUserId
            && assignment.CreatedByUserId != command.TeacherUserId
            && assignment.SolutionVideo?.UploadedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("Only the classroom teacher can delete this solution video.");
        }

        var mediaId = assignment.SolutionVideoMediaAssetId.Value;
        var storageKey = assignment.SolutionVideo?.StorageKey;

        assignment.SolutionVideoMediaAssetId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await MediaCleanup.TryDeleteOrphanMediaAsync(dbContext, fileStorage, mediaId, storageKey, cancellationToken);
        return true;
    }
}

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
