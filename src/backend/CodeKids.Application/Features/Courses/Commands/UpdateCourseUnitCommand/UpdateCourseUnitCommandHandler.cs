using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class UpdateCourseUnitCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateCourseUnitCommand, CourseUnitDto>
{
    public async Task<CourseUnitDto> Handle(UpdateCourseUnitCommand command, CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Unit title is required.");
        }

        var unit = await dbContext.CourseUnits
                .Include(x => x.Lessons)
                    .ThenInclude(x => x.Steps)
                .FirstOrDefaultAsync(x => x.Id == command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");

        unit.Title = title;
        unit.Description = (command.Description ?? string.Empty).Trim();
        unit.SortOrder = command.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);

        var lessons = unit.Lessons
            .OrderBy(x => x.SortOrder)
            .Select(l => new CourseLessonDto(
                l.Id,
                l.UnitId,
                l.Title,
                l.Theme,
                l.Description,
                l.Difficulty,
                l.XpReward,
                l.SortOrder,
                l.Steps.Count,
                l.StudentAskEnabled))
            .ToList();

        return new CourseUnitDto(
            unit.Id,
            unit.CourseId,
            unit.Title,
            unit.Description,
            unit.SortOrder,
            lessons,
            StudentAskEnabled: unit.StudentAskEnabled);
    }
}
