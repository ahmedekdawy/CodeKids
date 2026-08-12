using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed class GetParentDashboardQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    public async Task<ParentDashboardDto> Handle(GetParentDashboardQuery query, CancellationToken cancellationToken)
    {
        var parent = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ParentId && x.Role == UserRole.Parent, cancellationToken)
            ?? throw new InvalidOperationException("Parent account not found.");

        var children = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Badges)
                .ThenInclude(x => x.Badge)
            .Where(x => x.ParentId == parent.Id && x.Role == UserRole.Student)
            .ToListAsync(cancellationToken);

        var childIds = children.Select(x => x.Id).ToList();
        var progressCounts = await dbContext.StudentProgress
            .Where(x => childIds.Contains(x.UserId) && x.IsCompleted)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var quizCounts = await dbContext.QuizAttempts
            .Where(x => childIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        return new ParentDashboardDto(
            parent.Id,
            parent.DisplayName,
            children.Select(child => new ChildProgressDto(
                child.Id,
                child.DisplayName,
                child.TotalXp,
                progressCounts.GetValueOrDefault(child.Id),
                quizCounts.GetValueOrDefault(child.Id),
                child.AvatarId,
                child.Badges.Select(x => x.Badge!.Name).ToList())).ToList());
    }
}
