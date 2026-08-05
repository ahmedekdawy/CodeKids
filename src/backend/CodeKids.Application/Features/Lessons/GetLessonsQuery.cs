using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record LessonStepDto(
    Guid Id,
    int StepNumber,
    string Title,
    string Prompt);

public sealed record LessonDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int Difficulty,
    int XpReward,
    IReadOnlyList<LessonStepDto> Steps,
    IReadOnlyList<LessonVideoSummaryDto> Videos);

public sealed record LessonVideoSummaryDto(
    Guid Id,
    Guid MediaAssetId,
    string Title,
    int SortOrder,
    int? DurationSeconds);

public sealed record GetLessonsQuery(Guid? CourseId = null) : IQuery<IReadOnlyList<LessonDto>>;

public sealed record GetLessonByIdQuery(Guid LessonId) : IQuery<LessonDto?>;

public sealed class GetLessonsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>
{
    public async Task<IReadOnlyList<LessonDto>> Handle(GetLessonsQuery query, CancellationToken cancellationToken)
    {
        var lessonsQuery = dbContext.Lessons
            .AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Videos)
            .AsQueryable();

        if (query.CourseId is Guid courseId)
        {
            lessonsQuery = lessonsQuery.Where(x => x.CourseId == courseId);
        }

        var lessons = await lessonsQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Difficulty)
            .ToListAsync(cancellationToken);

        return lessons.Select(Map).ToList();
    }

    internal static LessonDto Map(Domain.Entities.Lesson lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            lesson.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new LessonStepDto(step.Id, step.StepNumber, step.Title, step.Prompt))
                .ToList(),
            lesson.Videos
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.CreatedAtUtc)
                .Select(v => new LessonVideoSummaryDto(
                    v.Id,
                    v.MediaAssetId,
                    v.Title,
                    v.SortOrder,
                    null))
                .ToList());
}

public sealed class GetLessonByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonByIdQuery, LessonDto?>
{
    public async Task<LessonDto?> Handle(GetLessonByIdQuery query, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Videos)
                .ThenInclude(v => v.MediaAsset)
            .FirstOrDefaultAsync(x => x.Id == query.LessonId, cancellationToken);

        if (lesson is null)
        {
            return null;
        }

        return new LessonDto(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            lesson.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new LessonStepDto(step.Id, step.StepNumber, step.Title, step.Prompt))
                .ToList(),
            lesson.Videos
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.CreatedAtUtc)
                .Select(v => new LessonVideoSummaryDto(
                    v.Id,
                    v.MediaAssetId,
                    v.Title,
                    v.SortOrder,
                    v.MediaAsset?.DurationSeconds))
                .ToList());
    }
}

