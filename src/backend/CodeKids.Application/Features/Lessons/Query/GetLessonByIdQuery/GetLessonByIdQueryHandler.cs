using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.StudentAsk;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

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
            .Include(x => x.Unit)
            .Include(x => x.Course)
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
                .ToList(),
            lesson.UnitId,
            StudentAskAccess.IsEnabled(lesson.Course, lesson.Unit, lesson));
    }
}
