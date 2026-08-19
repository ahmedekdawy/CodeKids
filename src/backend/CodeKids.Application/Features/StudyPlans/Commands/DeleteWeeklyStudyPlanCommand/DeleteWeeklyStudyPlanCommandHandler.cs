using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class DeleteWeeklyStudyPlanCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteWeeklyStudyPlanCommand, bool>
{
    public async Task<bool> Handle(
        DeleteWeeklyStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.WeeklyStudyPlans
            .FirstOrDefaultAsync(
                x => x.Id == command.PlanId && x.TeacherId == command.TeacherId,
                cancellationToken)
            ?? throw new InvalidOperationException("Study plan not found.");

        dbContext.WeeklyStudyPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
