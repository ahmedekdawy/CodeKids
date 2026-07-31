using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

public sealed record AvatarDto(
    Guid Id,
    string Name,
    string Theme,
    string AccentColor,
    string Emoji,
    int UnlockXp,
    bool IsUnlocked,
    bool IsSelected);

public sealed record SelectAvatarRequest(Guid AvatarId);

public sealed record GetAvatarsQuery(Guid UserId) : IQuery<IReadOnlyList<AvatarDto>>;

public sealed record SelectAvatarCommand(Guid UserId, Guid AvatarId) : ICommand<AvatarDto>;

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

public sealed class SelectAvatarCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SelectAvatarCommand, AvatarDto>
{
    public async Task<AvatarDto> Handle(SelectAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var avatar = await dbContext.Avatars.FirstOrDefaultAsync(x => x.Id == command.AvatarId, cancellationToken)
            ?? throw new InvalidOperationException("Avatar not found.");

        if (user.TotalXp < avatar.UnlockXp)
        {
            throw new InvalidOperationException($"Earn {avatar.UnlockXp} XP to unlock {avatar.Name}.");
        }

        user.AvatarId = avatar.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AvatarDto(
            avatar.Id,
            avatar.Name,
            avatar.Theme,
            avatar.AccentColor,
            avatar.Emoji,
            avatar.UnlockXp,
            true,
            true);
    }
}

