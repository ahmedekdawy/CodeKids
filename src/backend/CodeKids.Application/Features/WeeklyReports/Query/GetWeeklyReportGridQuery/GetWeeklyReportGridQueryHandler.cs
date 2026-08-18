using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.WeeklyReports;

public sealed class GetWeeklyReportGridQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetWeeklyReportGridQuery, IReadOnlyList<StudentWeeklyReportGridRowDto>>
{
    public async Task<IReadOnlyList<StudentWeeklyReportGridRowDto>> Handle(
        GetWeeklyReportGridQuery query,
        CancellationToken cancellationToken)
    {
        var studentIds = await WeeklyReportAccess.GetTeacherStudentIdsAsync(
            dbContext, query.TeacherId, query.Grade, cancellationToken);

        if (studentIds.Count == 0)
        {
            return [];
        }

        var students = await dbContext.Users
            .AsNoTracking()
            .Where(x => studentIds.Contains(x.Id) && x.Role == UserRole.Student)
            .OrderBy(x => x.Grade)
            .ThenBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.Grade })
            .ToListAsync(cancellationToken);

        var existing = await dbContext.StudentWeeklyReports
            .AsNoTracking()
            .Where(x => x.TeacherId == query.TeacherId && x.WeekStartDate == query.WeekStartDate)
            .Where(x => studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        return students.Select(student =>
        {
            existing.TryGetValue(student.Id, out var report);
            return new StudentWeeklyReportGridRowDto(
                report?.Id,
                student.Id,
                student.DisplayName,
                student.Grade,
                query.WeekStartDate,
                report?.PerformancePercent,
                report?.AttendancePercent,
                report?.HomeworkPercent,
                report?.InteractionDuringSession ?? string.Empty,
                report?.OpenCamera);
        }).ToList();
    }
}
