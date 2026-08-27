using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

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

        var found = await CourseOutlineResolver.FindUnitAsync(dbContext, command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");
        found.Unit.Title = CourseOutlineResolver.Clamp(title, 300);
        found.Unit.SortOrder = Math.Max(1, command.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseOutlineResolver.MapUnit(found.Course, found.Subject, found.Unit);
    }
}
