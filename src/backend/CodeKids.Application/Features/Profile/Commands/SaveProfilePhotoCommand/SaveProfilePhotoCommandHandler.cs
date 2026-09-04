using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Profile;

public sealed class SaveProfilePhotoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<SaveProfilePhotoCommand, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(SaveProfilePhotoCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        var previousStorageKey = user.ProfilePhotoStorageKey;
        user.ProfilePhotoStorageKey = command.StorageKey;
        user.ProfilePhotoContentType = command.ContentType;
        await dbContext.SaveChangesAsync(cancellationToken);

        await ProfilePhotoStorage.DeleteQuietlyAsync(fileStorage, previousStorageKey, cancellationToken);
        return RegisterCommandHandler.ToDto(user);
    }
}

public sealed class RemoveProfilePhotoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<RemoveProfilePhotoCommand, AuthUserDto>
{
    public async Task<AuthUserDto> Handle(RemoveProfilePhotoCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        var previousStorageKey = user.ProfilePhotoStorageKey;
        user.ProfilePhotoStorageKey = null;
        user.ProfilePhotoContentType = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await ProfilePhotoStorage.DeleteQuietlyAsync(fileStorage, previousStorageKey, cancellationToken);
        return RegisterCommandHandler.ToDto(user);
    }
}

internal static class ProfilePhotoStorage
{
    /// <summary>A stale file left behind must never fail the request that replaced it.</summary>
    public static async Task DeleteQuietlyAsync(
        IFileStorage fileStorage,
        string? storageKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return;

        try
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
        catch
        {
            // ignored
        }
    }
}
