using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class DeleteCourseLessonCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseLessonCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseLessonCommand command, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
                .FirstOrDefaultAsync(x => x.Id == command.LessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");

        dbContext.Lessons.Remove(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
