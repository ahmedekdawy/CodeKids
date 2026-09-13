using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Tenants;

public sealed record AdminTenantDto(
    string Id,
    string Name,
    string Slug,
    string Email,
    string MobilePhone,
    string DisplayName,
    string Status,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    int UserCount,
    DateTimeOffset? LastLoginUtc);

public sealed record ListTenantsQuery : IQuery<IReadOnlyList<AdminTenantDto>>;
