using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed class DeleteCourseLessonCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseLessonCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseLessonCommand command, CancellationToken cancellationToken)
    {
        var found = await CourseOutlineResolver.FindLessonAsync(dbContext, command.LessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");
        dbContext.SubjectUnitLessons.Remove(found.Lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
