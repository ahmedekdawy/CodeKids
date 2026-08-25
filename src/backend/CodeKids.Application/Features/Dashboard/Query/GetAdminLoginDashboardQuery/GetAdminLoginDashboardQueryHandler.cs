using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed class GetAdminLoginDashboardQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAdminLoginDashboardQuery, AdminLoginDashboardDto>
{
    private static readonly UserRole[] TrackedRoles =
        [UserRole.Teacher, UserRole.Parent, UserRole.Student];

    public async Task<AdminLoginDashboardDto> Handle(
        GetAdminLoginDashboardQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ToDate < query.FromDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var fromUtc = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusive = new DateTimeOffset(query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var logins = await dbContext.Users
            .AsNoTracking()
            .Where(x =>
                x.LastLoginDateUtc != null
                && x.LastLoginDateUtc >= fromUtc
                && x.LastLoginDateUtc < toExclusive
                && TrackedRoles.Contains(x.Role))
            .Select(x => new { x.Role, LoginAt = x.LastLoginDateUtc!.Value })
            .ToListAsync(cancellationToken);

        var byDayRole = logins
            .GroupBy(x => (Date: DateOnly.FromDateTime(x.LoginAt.UtcDateTime), x.Role))
            .ToDictionary(g => g.Key, g => g.Count());

        var days = new List<AdminLoginDashboardDayDto>();
        for (var date = query.FromDate; date <= query.ToDate; date = date.AddDays(1))
        {
            days.Add(new AdminLoginDashboardDayDto(
                date,
                byDayRole.GetValueOrDefault((date, UserRole.Teacher)),
                byDayRole.GetValueOrDefault((date, UserRole.Parent)),
                byDayRole.GetValueOrDefault((date, UserRole.Student))));
        }

        return new AdminLoginDashboardDto(
            query.FromDate,
            query.ToDate,
            days.Sum(d => d.Teachers),
            days.Sum(d => d.Parents),
            days.Sum(d => d.Students),
            days);
    }
}
