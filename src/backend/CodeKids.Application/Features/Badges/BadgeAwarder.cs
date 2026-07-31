using CodeKids.Domain.Entities;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Badges;

public static class BadgeAwarder
{
    public static async Task AwardEligibleAsync(IAppDbContext dbContext, User user, CancellationToken cancellationToken)
    {
        var completedSteps = await dbContext.StudentProgress
            .CountAsync(x => x.UserId == user.Id && x.IsCompleted, cancellationToken);

        var ownedBadgeIds = await dbContext.UserBadges
            .Where(x => x.UserId == user.Id)
            .Select(x => x.BadgeId)
            .ToListAsync(cancellationToken);

        var eligible = await dbContext.Badges
            .Where(x => !ownedBadgeIds.Contains(x.Id)
                        && user.TotalXp >= x.RequiredXp
                        && completedSteps >= x.RequiredSteps)
            .ToListAsync(cancellationToken);

        foreach (var badge in eligible)
        {
            dbContext.UserBadges.Add(new UserBadge
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BadgeId = badge.Id,
                AwardedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (eligible.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

