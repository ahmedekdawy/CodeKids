using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Tenants;

public sealed record RegisterTenantRequest(
    string TenantName,
    string Email,
    string DisplayName,
    string Password,
    string? MobilePhone = null,
    string? TenantStyle = null,
    bool? MultiTimeZone = null,
    string? TimeZone = null);

public sealed record RegisterTenantResult(bool Accepted, string Message);

public sealed record RegisterTenantCommand(
    string TenantName,
    string Email,
    string DisplayName,
    string Password,
    string? MobilePhone = null,
    string? TenantStyle = null,
    bool MultiTimeZone = false,
    string? TimeZone = null) : ICommand<RegisterTenantResult>;
