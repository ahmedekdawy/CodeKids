using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.Courses;

public sealed class CreateCourseLessonCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseLessonCommand, CourseLessonDto>
{
    public async Task<CourseLessonDto> Handle(CreateCourseLessonCommand command, CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Lesson title is required.");
        }

        var found = await CourseOutlineResolver.FindUnitAsync(dbContext, command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");
        var lesson = new SubjectUnitLesson
        {
            SubjectUnitId = found.Unit.Id,
            Title = CourseOutlineResolver.Clamp(title, 300),
            SortOrder = Math.Max(1, command.SortOrder)
        };
        dbContext.SubjectUnitLessons.Add(lesson);
        found.Unit.Lessons.Add(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseOutlineResolver.MapLesson(found.Course, found.Subject, found.Unit, lesson);
    }
}
