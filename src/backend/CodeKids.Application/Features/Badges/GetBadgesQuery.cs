using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Badges;

public sealed record BadgeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Icon,
    int RequiredXp,
    int RequiredSteps,
    bool IsEarned);

public sealed record GetBadgesQuery(Guid UserId) : IQuery<IReadOnlyList<BadgeDto>>;

public sealed class GetBadgesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>>
{
    public async Task<IReadOnlyList<BadgeDto>> Handle(GetBadgesQuery query, CancellationToken cancellationToken)
    {
        var earned = await dbContext.UserBadges
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => x.BadgeId)
            .ToListAsync(cancellationToken);

        var badges = await dbContext.Badges
            .AsNoTracking()
            .OrderBy(x => x.RequiredXp)
            .ToListAsync(cancellationToken);

        return badges.Select(badge => new BadgeDto(
            badge.Id,
            badge.Code,
            badge.Name,
            badge.Description,
            badge.Icon,
            badge.RequiredXp,
            badge.RequiredSteps,
            earned.Contains(badge.Id))).ToList();
    }
}

