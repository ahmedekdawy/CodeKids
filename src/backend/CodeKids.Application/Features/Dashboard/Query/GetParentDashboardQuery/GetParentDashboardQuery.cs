using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Dashboard;

public sealed record GetParentDashboardQuery(Guid ParentId) : IQuery<ParentDashboardDto>;
