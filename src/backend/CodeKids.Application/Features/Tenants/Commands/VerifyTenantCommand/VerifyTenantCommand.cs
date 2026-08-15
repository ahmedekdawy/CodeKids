using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Tenants;

public sealed record VerifyTenantRequest(string Token);

public sealed record VerifyTenantResult(string TenantId, string Email, string Message);

public sealed record VerifyTenantCommand(string Token) : ICommand<VerifyTenantResult>;
