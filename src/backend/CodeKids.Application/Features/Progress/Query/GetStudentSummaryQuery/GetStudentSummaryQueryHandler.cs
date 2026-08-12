using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Progress;

public sealed class GetStudentSummaryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetStudentSummaryQuery, StudentSummaryDto>
{
    public async Task<StudentSummaryDto> Handle(GetStudentSummaryQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Badges)
                .ThenInclude(x => x.Badge)
            .FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var completedSteps = await dbContext.StudentProgress
            .CountAsync(x => x.UserId == query.UserId && x.IsCompleted, cancellationToken);

        return new StudentSummaryDto(
            user.Id,
            user.DisplayName,
            completedSteps,
            user.TotalXp,
            user.AvatarId,
            user.Badges.Select(x => x.Badge!.Name).ToList());
    }
}
