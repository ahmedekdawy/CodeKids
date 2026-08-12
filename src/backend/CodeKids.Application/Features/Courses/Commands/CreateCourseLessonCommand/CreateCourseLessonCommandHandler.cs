using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        var theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        if (command.Difficulty is < 1 or > 5)
        {
            throw new InvalidOperationException("Difficulty must be between 1 and 5.");
        }

        if (command.XpReward < 0)
        {
            throw new InvalidOperationException("XP reward cannot be negative.");
        }

        var unit = await dbContext.CourseUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = unit.CourseId,
            UnitId = unit.Id,
            Title = title,
            Theme = theme,
            Description = (command.Description ?? string.Empty).Trim(),
            Difficulty = command.Difficulty,
            XpReward = command.XpReward,
            SortOrder = command.SortOrder
        };

        dbContext.Lessons.Add(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CourseLessonDto(
            lesson.Id,
            lesson.UnitId,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            lesson.SortOrder,
            0);
    }
}
