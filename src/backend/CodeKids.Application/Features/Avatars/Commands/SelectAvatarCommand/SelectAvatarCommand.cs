using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

public sealed record SelectAvatarRequest(Guid AvatarId);

public sealed record SelectAvatarCommand(Guid UserId, Guid AvatarId) : ICommand<AvatarDto>;
