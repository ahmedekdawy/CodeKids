using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed class UpdateCourseLessonCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateCourseLessonCommand, CourseLessonDto>
{
    public async Task<CourseLessonDto> Handle(UpdateCourseLessonCommand command, CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Lesson title is required.");
        }

        var found = await CourseOutlineResolver.FindLessonAsync(dbContext, command.LessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");
        if (command.UnitId is Guid unitId && unitId != CourseOutlineResolver.UnitId(found.Course, found.Subject, found.Unit))
        {
            var target = await CourseOutlineResolver.FindUnitAsync(dbContext, unitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            found.Lesson.SubjectUnitId = target.Unit.Id;
            found = found with { Subject = target.Subject, Unit = target.Unit };
        }

        found.Lesson.Title = CourseOutlineResolver.Clamp(title, 300);
        found.Lesson.SortOrder = Math.Max(1, command.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseOutlineResolver.MapLesson(found.Course, found.Subject, found.Unit, found.Lesson);
    }
}
