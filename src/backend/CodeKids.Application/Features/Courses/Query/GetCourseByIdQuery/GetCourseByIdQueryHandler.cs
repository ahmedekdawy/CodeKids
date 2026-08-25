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
            .Include(x => x.Units)
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Steps)
            .Include(x => x.Quizzes)
                .ThenInclude(x => x.Questions)
            .Where(x => x.Id == query.CourseId);

        coursesQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, coursesQuery, query.UserId, query.Role, cancellationToken);

        var course = await coursesQuery.FirstOrDefaultAsync(cancellationToken);
        return course is null ? null : CourseDtoMapper.Map(course);
    }
}
