using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Admin;

public sealed record SetManagedUserActiveRequest(bool IsActive);

public sealed record SetManagedUserActiveCommand(Guid AdminUserId, Guid UserId, bool IsActive)
    : ICommand<ManagedUserDto>;
