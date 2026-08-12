using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

public sealed class GetAvatarsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAvatarsQuery, IReadOnlyList<AvatarDto>>
{
    public async Task<IReadOnlyList<AvatarDto>> Handle(GetAvatarsQuery query, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var avatars = await dbContext.Avatars
            .AsNoTracking()
            .OrderBy(x => x.UnlockXp)
            .ToListAsync(cancellationToken);

        return avatars.Select(avatar => new AvatarDto(
            avatar.Id,
            avatar.Name,
            avatar.Theme,
            avatar.AccentColor,
            avatar.Emoji,
            avatar.UnlockXp,
            user.TotalXp >= avatar.UnlockXp,
            user.AvatarId == avatar.Id)).ToList();
    }
}
