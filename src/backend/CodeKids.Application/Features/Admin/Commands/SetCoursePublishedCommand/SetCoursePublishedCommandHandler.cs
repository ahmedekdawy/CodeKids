using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class SetCoursePublishedCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SetCoursePublishedCommand, CourseSummaryDto>
{
    public async Task<CourseSummaryDto> Handle(
        SetCoursePublishedCommand command,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        course.IsPublished = command.IsPublished;
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateCourseCommandHandler.ToSummary(course);
    }
}
