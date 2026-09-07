using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Auth;

public sealed record ImpersonateUserCommand(Guid AdminUserId, Guid TargetUserId) : ICommand<AuthResponse>;
