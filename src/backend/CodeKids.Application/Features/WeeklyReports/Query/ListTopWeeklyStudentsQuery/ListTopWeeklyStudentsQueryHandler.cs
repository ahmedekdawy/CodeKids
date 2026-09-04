using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.WeeklyReports;

public sealed class ListTopWeeklyStudentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTopWeeklyStudentsQuery, IReadOnlyList<TopWeeklyStudentDto>>
{
    public const int MinPerformancePercent = 90;

    public async Task<IReadOnlyList<TopWeeklyStudentDto>> Handle(
        ListTopWeeklyStudentsQuery query,
        CancellationToken cancellationToken)
    {
        var weekStart = query.WeekStartDate ?? StartOfWeek(DateOnly.FromDateTime(DateTime.UtcNow));

        var rows = await dbContext.StudentWeeklyReports
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.WeekStartDate == weekStart)
            .Where(x => x.PerformancePercent != null && x.PerformancePercent.Value >= MinPerformancePercent)
            .Where(x => x.Student != null && x.Student.IsActive)
            .Select(x => new
            {
                StudentId = x.StudentId,
                StudentName = x.Student!.DisplayName,
                StudentGrade = x.Student.Grade,
                PerformancePercent = x.PerformancePercent!.Value
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.StudentId)
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.PerformancePercent).First();
                return new TopWeeklyStudentDto(
                    best.StudentName,
                    best.StudentGrade,
                    best.PerformancePercent,
                    weekStart);
            })
            .OrderByDescending(x => x.PerformancePercent)
            .ThenBy(x => x.StudentName)
            .ToList();
    }

    internal static DateOnly StartOfWeek(DateOnly date)
    {
        // Monday-based school week (matches teacher UI).
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }
}
