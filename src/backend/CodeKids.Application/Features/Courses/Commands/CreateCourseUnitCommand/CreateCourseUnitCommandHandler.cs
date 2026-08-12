using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class CreateCourseUnitCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseUnitCommand, CourseUnitDto>
{
    public async Task<CourseUnitDto> Handle(CreateCourseUnitCommand command, CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Unit title is required.");
        }

        _ = await dbContext.Courses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var unit = new CourseUnit
        {
            Id = Guid.NewGuid(),
            CourseId = command.CourseId,
            Title = title,
            Description = (command.Description ?? string.Empty).Trim(),
            SortOrder = command.SortOrder
        };

        dbContext.CourseUnits.Add(unit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CourseUnitDto(unit.Id, unit.CourseId, unit.Title, unit.Description, unit.SortOrder, []);
    }
}
