using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Dashboard;

public sealed record GetAdminLoginDashboardQuery(
    DateOnly FromDate,
    DateOnly ToDate) : IQuery<AdminLoginDashboardDto>;
