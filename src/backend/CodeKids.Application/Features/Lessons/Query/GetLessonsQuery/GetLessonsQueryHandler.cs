using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Application.Features.Courses;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed class GetLessonsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>
{
    public async Task<IReadOnlyList<LessonDto>> Handle(GetLessonsQuery query, CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses.AsNoTracking().AsQueryable();
        if (query.CourseId is Guid courseId)
        {
            coursesQuery = coursesQuery.Where(x => x.Id == courseId);
        }

        var courses = await coursesQuery.ToListAsync(cancellationToken);
        var outlines = await CourseOutlineResolver.ResolveManyAsync(dbContext, courses, cancellationToken);
        var result = new List<LessonDto>();
        foreach (var course in courses)
        {
            var outline = outlines[course.Id];
            foreach (var unit in outline.Units)
            {
                foreach (var lesson in unit.Lessons)
                {
                    result.Add(await CourseOutlineResolver.ToPlayableAsync(
                        dbContext, course, lesson, unit.StudentAskEnabled, cancellationToken));
                }
            }
        }

        return result
            .OrderBy(x => x.Difficulty)
            .ThenBy(x => x.Title)
            .ToList();
    }
}
