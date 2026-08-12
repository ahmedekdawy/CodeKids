using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Avatars;

public sealed record GetAvatarsQuery(Guid UserId) : IQuery<IReadOnlyList<AvatarDto>>;
