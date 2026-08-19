using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Auth;

public sealed record UpdateOwnAccountRequest(
    string? Email,
    string? MobilePhone,
    string? Password);

public sealed record UpdateOwnAccountCommand(
    Guid UserId,
    string? Email,
    string? MobilePhone,
    string? Password) : ICommand<AuthUserDto>;
