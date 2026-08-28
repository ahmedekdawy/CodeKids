using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed class DeleteCourseUnitCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseUnitCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseUnitCommand command, CancellationToken cancellationToken)
    {
        var found = await CourseOutlineResolver.FindUnitAsync(dbContext, command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");
        dbContext.SubjectUnits.Remove(found.Unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
