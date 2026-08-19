using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Dashboard;

public sealed record GetParentChildOverviewQuery(Guid ParentId, Guid ChildId) : IQuery<ParentChildOverviewDto>;
