using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class SetStudentAskEnabledCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SetStudentAskEnabledCommand, StudentAskSettingsDto>
{
    public async Task<StudentAskSettingsDto> Handle(
        SetStudentAskEnabledCommand command,
        CancellationToken cancellationToken)
    {
        var scope = (command.Scope ?? string.Empty).Trim().ToLowerInvariant();
        if (scope is not ("course" or "unit" or "lesson"))
        {
            throw new InvalidOperationException("Ask scope must be course, unit, or lesson.");
        }

        if (scope == "course")
        {
            var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");
            course.StudentAskEnabled = command.Enabled;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new StudentAskSettingsDto("course", course.Id, course.StudentAskEnabled);
        }

        if (scope == "unit")
        {
            var found = await CourseOutlineResolver.FindUnitAsync(dbContext, command.Id, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            found.Unit.StudentAskEnabled = command.Enabled;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new StudentAskSettingsDto("unit", command.Id, found.Unit.StudentAskEnabled);
        }

        var lesson = await CourseOutlineResolver.FindLessonAsync(dbContext, command.Id, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");
        lesson.Lesson.StudentAskEnabled = command.Enabled;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new StudentAskSettingsDto("lesson", command.Id, lesson.Lesson.StudentAskEnabled);
    }
}
