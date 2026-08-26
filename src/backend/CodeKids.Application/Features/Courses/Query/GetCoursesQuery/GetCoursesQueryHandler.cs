using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class GetCoursesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Course> coursesQuery = dbContext.Courses.AsNoTracking();

        if (query.IncludeContent)
        {
            coursesQuery = coursesQuery
                .Include(x => x.Units)
                .Include(x => x.Lessons)
                    .ThenInclude(x => x.Steps)
                .Include(x => x.Quizzes)
                    .ThenInclude(x => x.Questions);
        }

        coursesQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, coursesQuery, query.UserId, query.Role, cancellationToken);

        var courses = await coursesQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Grade)
            .ThenBy(x => x.Title)
            .ThenBy(x => x.TrackName)
            .ToListAsync(cancellationToken);

        if (!query.IncludeContent)
        {
            return courses.Select(course => CourseDtoMapper.Map(course, includeContent: false)).ToList();
        }

        var outlines = await CourseOutlineResolver.ResolveManyAsync(dbContext, courses, cancellationToken);
        return courses.Select(course => CourseDtoMapper.Map(course, query.IncludeContent, outlines[course.Id])).ToList();
    }
}
