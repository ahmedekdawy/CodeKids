using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class GetCourseByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCourseByIdQuery, CourseDto?>
{
    public async Task<CourseDto?> Handle(GetCourseByIdQuery query, CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Quizzes)
                .ThenInclude(x => x.Questions)
            .Where(x => x.Id == query.CourseId);

        coursesQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, coursesQuery, query.UserId, query.Role, cancellationToken);

        var course = await coursesQuery.FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return null;
        }

        var outline = await CourseOutlineResolver.ResolveAsync(dbContext, course, cancellationToken);
        return CourseDtoMapper.Map(course, includeContent: true, outline);
    }
}
