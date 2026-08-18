using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.WeeklyReports;

public sealed class SaveWeeklyReportsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SaveWeeklyReportsCommand, IReadOnlyList<StudentWeeklyReportGridRowDto>>
{
    public async Task<IReadOnlyList<StudentWeeklyReportGridRowDto>> Handle(
        SaveWeeklyReportsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Entries.Count == 0)
        {
            return [];
        }

        ValidateEntries(command.Entries);

        var studentIds = command.Entries.Select(x => x.StudentId).Distinct().ToList();
        await WeeklyReportAccess.EnsureTeacherOwnsStudentsAsync(
            dbContext, command.TeacherId, studentIds, cancellationToken);

        var existing = await dbContext.StudentWeeklyReports
            .Where(x => x.TeacherId == command.TeacherId && x.WeekStartDate == command.WeekStartDate)
            .Where(x => studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in command.Entries)
        {
            if (IsEmpty(entry))
            {
                if (existing.TryGetValue(entry.StudentId, out var toRemove))
                {
                    dbContext.StudentWeeklyReports.Remove(toRemove);
                    existing.Remove(entry.StudentId);
                }

                continue;
            }

            if (existing.TryGetValue(entry.StudentId, out var row))
            {
                row.PerformancePercent = entry.PerformancePercent;
                row.AttendancePercent = entry.AttendancePercent;
                row.HomeworkPercent = entry.HomeworkPercent;
                row.InteractionDuringSession = entry.InteractionDuringSession?.Trim() ?? string.Empty;
                row.OpenCamera = entry.OpenCamera;
                row.UpdatedAtUtc = now;
            }
            else
            {
                dbContext.StudentWeeklyReports.Add(new StudentWeeklyReport
                {
                    Id = Guid.NewGuid(),
                    TeacherId = command.TeacherId,
                    StudentId = entry.StudentId,
                    WeekStartDate = command.WeekStartDate,
                    PerformancePercent = entry.PerformancePercent,
                    AttendancePercent = entry.AttendancePercent,
                    HomeworkPercent = entry.HomeworkPercent,
                    InteractionDuringSession = entry.InteractionDuringSession?.Trim() ?? string.Empty,
                    OpenCamera = entry.OpenCamera,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await new GetWeeklyReportGridQueryHandler(dbContext).Handle(
            new GetWeeklyReportGridQuery(command.TeacherId, command.WeekStartDate, Grade: null),
            cancellationToken);
    }

    private static void ValidateEntries(IReadOnlyList<SaveWeeklyReportEntryDto> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.PerformancePercent is < 0 or > 100)
            {
                throw new InvalidOperationException("Performance percent must be between 0 and 100.");
            }

            if (entry.AttendancePercent is < 0 or > 100)
            {
                throw new InvalidOperationException("Attendance percent must be between 0 and 100.");
            }

            if (entry.HomeworkPercent is < 0 or > 100)
            {
                throw new InvalidOperationException("Homework percent must be between 0 and 100.");
            }
        }
    }

    private static bool IsEmpty(SaveWeeklyReportEntryDto entry) =>
        entry.PerformancePercent is null
        && entry.AttendancePercent is null
        && entry.HomeworkPercent is null
        && string.IsNullOrWhiteSpace(entry.InteractionDuringSession)
        && entry.OpenCamera is null;
}
