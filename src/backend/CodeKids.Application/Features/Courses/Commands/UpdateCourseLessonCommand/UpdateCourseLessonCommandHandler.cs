using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

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

        var theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        if (command.Difficulty is < 1 or > 5)
        {
            throw new InvalidOperationException("Difficulty must be between 1 and 5.");
        }

        if (command.XpReward < 0)
        {
            throw new InvalidOperationException("XP reward cannot be negative.");
        }

        var lesson = await dbContext.Lessons
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(x => x.Id == command.LessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");

        if (command.UnitId.HasValue && command.UnitId.Value != lesson.UnitId)
        {
            var unit = await dbContext.CourseUnits
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == command.UnitId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");

            if (unit.CourseId != lesson.CourseId)
            {
                throw new InvalidOperationException("Unit must belong to the same course.");
            }

            lesson.UnitId = unit.Id;
        }

        lesson.Title = title;
        lesson.Theme = theme;
        lesson.Description = (command.Description ?? string.Empty).Trim();
        lesson.Difficulty = command.Difficulty;
        lesson.XpReward = command.XpReward;
        lesson.SortOrder = command.SortOrder;
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
            lesson.Steps.Count,
            lesson.StudentAskEnabled);
    }
}
