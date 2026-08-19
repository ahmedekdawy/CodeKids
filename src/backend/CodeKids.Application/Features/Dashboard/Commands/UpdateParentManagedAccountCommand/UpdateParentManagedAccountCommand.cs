using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Dashboard;

public sealed record ParentManagedAccountDto(
    Guid UserId,
    string DisplayName,
    string Role,
    string Email,
    string MobilePhone);

public sealed record UpdateParentManagedAccountRequest(
    string? Email,
    string? MobilePhone,
    string? Password);

public sealed record UpdateParentManagedAccountCommand(
    Guid ParentId,
    Guid TargetUserId,
    string? Email,
    string? MobilePhone,
    string? Password) : ICommand<ParentManagedAccountDto>;
