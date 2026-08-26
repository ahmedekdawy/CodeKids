using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.StudentAsk;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed class GetLessonsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>
{
    public async Task<IReadOnlyList<LessonDto>> Handle(GetLessonsQuery query, CancellationToken cancellationToken)
    {
        var lessonsQuery = dbContext.Lessons
            .AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Videos)
            .Include(x => x.Unit)
            .Include(x => x.Course)
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
                .ToList(),
            lesson.UnitId,
            StudentAskAccess.IsEnabled(lesson.Course, lesson.Unit, lesson));
}
