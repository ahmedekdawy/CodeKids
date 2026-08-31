using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

internal static class CourseVideoLoader
{
    public static async Task<Dictionary<Guid, IReadOnlyList<CourseVideoSummaryDto>>> LoadByCourseIdsAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        if (courseIds.Count == 0)
        {
            return [];
        }

        var targetIds = courseIds.ToHashSet();
        var videos = await dbContext.LessonVideos
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Where(x => x.LessonId == null && x.CourseId != null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (videos.Count == 0)
        {
            return [];
        }

        var targets = await LoadKeysAsync(dbContext, targetIds, cancellationToken);
        var sourceIds = videos.Select(v => v.CourseId!.Value).ToHashSet();
        sourceIds.ExceptWith(targetIds);
        var sources = await LoadKeysAsync(dbContext, sourceIds, cancellationToken);
        var sourceById = sources.ToDictionary(c => c.Id);

        var result = new Dictionary<Guid, IReadOnlyList<CourseVideoSummaryDto>>();
        foreach (var target in targets)
        {
            var matched = videos
                .Where(video => MatchesTarget(target, video.CourseId!.Value, sourceById))
                .Select(ToSummary)
                .ToList();
            if (matched.Count > 0)
            {
                result[target.Id] = matched;
            }
        }

        return result;
    }

    public static async Task<HashSet<Guid>> GetRelatedCourseIdsAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        var related = courseIds.ToHashSet();
        if (related.Count == 0)
        {
            return related;
        }

        var targets = await LoadKeysAsync(dbContext, related, cancellationToken);
        var videoCourseIds = await dbContext.LessonVideos
            .AsNoTracking()
            .Where(x => x.LessonId == null && x.CourseId != null)
            .Select(x => x.CourseId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (videoCourseIds.Count == 0)
        {
            return related;
        }

        var sources = await LoadKeysAsync(dbContext, videoCourseIds, cancellationToken);
        foreach (var source in sources)
        {
            if (targets.Any(target => AreEquivalent(target, source)))
            {
                related.Add(source.Id);
            }
        }

        return related;
    }

    public static CourseVideoSummaryDto ToSummary(LessonVideo video) =>
        new(video.Id, video.MediaAssetId, video.Title, video.SortOrder, video.MediaAsset?.DurationSeconds);

    private static bool MatchesTarget(
        CourseKey target,
        Guid videoCourseId,
        IReadOnlyDictionary<Guid, CourseKey> sourceById)
    {
        if (videoCourseId == target.Id)
        {
            return true;
        }

        return sourceById.TryGetValue(videoCourseId, out var source) && AreEquivalent(target, source);
    }

    private static bool AreEquivalent(CourseKey left, CourseKey right)
    {
        if (left.Id == right.Id)
        {
            return true;
        }

        if (!GradeStageHelper.CourseAudiencesOverlap(
                left.Grade, left.StageId, right.Grade, right.StageId))
        {
            return false;
        }

        if (!StudentCourseVisibility.MatchesStudentSchoolType(left.SchoolType, right.SchoolType))
        {
            return false;
        }

        if (left.ExternalSubjectId is int leftSubject
            && right.ExternalSubjectId is int rightSubject
            && leftSubject == rightSubject)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(left.SubjectCode)
            && !string.IsNullOrWhiteSpace(right.SubjectCode)
            && string.Equals(left.SubjectCode, right.SubjectCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var leftTitle = NormalizeTitle(left.Title);
        var rightTitle = NormalizeTitle(right.Title);
        return leftTitle.Length >= 3 && leftTitle == rightTitle;
    }

    private static string NormalizeTitle(string title)
    {
        var value = string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();
        if (value.StartsWith("ال", StringComparison.Ordinal) && value.Length > 3)
        {
            value = value[2..];
        }

        return value;
    }

    private static async Task<List<CourseKey>> LoadKeysAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        if (courseIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Courses
            .AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new CourseKey(
                c.Id,
                c.Title,
                c.Grade,
                c.StageId,
                c.SchoolType,
                c.SubjectCode,
                c.ExternalSubjectId))
            .ToListAsync(cancellationToken);
    }

    private sealed record CourseKey(
        Guid Id,
        string Title,
        int? Grade,
        int? StageId,
        SchoolType? SchoolType,
        string SubjectCode,
        int? ExternalSubjectId);
}
