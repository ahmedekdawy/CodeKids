using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

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
