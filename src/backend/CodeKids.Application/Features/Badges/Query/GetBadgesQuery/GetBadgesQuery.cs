using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Badges;

public sealed record GetBadgesQuery(Guid UserId) : IQuery<IReadOnlyList<BadgeDto>>;
