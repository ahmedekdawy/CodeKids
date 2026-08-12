using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class DeleteCourseUnitCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseUnitCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await dbContext.CourseUnits
                .FirstOrDefaultAsync(x => x.Id == command.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");

        dbContext.CourseUnits.Remove(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
