using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Profile;

public sealed record SaveProfilePhotoCommand(
    Guid UserId,
    string StorageKey,
    string ContentType) : ICommand<AuthUserDto>;

public sealed record RemoveProfilePhotoCommand(Guid UserId) : ICommand<AuthUserDto>;
