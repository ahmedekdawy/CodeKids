using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Lessons;

public sealed class GetLessonByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLessonByIdQuery, LessonDto?>
{
    public Task<LessonDto?> Handle(GetLessonByIdQuery query, CancellationToken cancellationToken) =>
        CourseOutlineResolver.ResolvePlayableLessonAsync(dbContext, query.LessonId, cancellationToken);
}
