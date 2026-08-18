using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.WeeklyReports;

public sealed class ListStudentWeeklyReportsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListStudentWeeklyReportsQuery, IReadOnlyList<StudentWeeklyReportDto>>
{
    public async Task<IReadOnlyList<StudentWeeklyReportDto>> Handle(
        ListStudentWeeklyReportsQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.StudentWeeklyReports
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.TeacherId == query.TeacherId)
            .AsQueryable();

        if (query.Grade.HasValue)
        {
            rows = rows.Where(x => x.Student != null && x.Student.Grade == query.Grade.Value);
        }

        if (query.FromDate.HasValue)
        {
            rows = rows.Where(x => x.WeekStartDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            rows = rows.Where(x => x.WeekStartDate <= query.ToDate.Value);
        }

        return (await rows
            .OrderByDescending(x => x.WeekStartDate)
            .ThenBy(x => x.Student!.Grade)
            .ThenBy(x => x.Student!.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(x => new StudentWeeklyReportDto(
                x.Id,
                x.TeacherId,
                x.StudentId,
                x.Student?.DisplayName ?? string.Empty,
                x.Student?.Grade,
                x.WeekStartDate,
                x.PerformancePercent,
                x.AttendancePercent,
                x.HomeworkPercent,
                x.InteractionDuringSession,
                x.OpenCamera))
            .ToList();
    }
}
