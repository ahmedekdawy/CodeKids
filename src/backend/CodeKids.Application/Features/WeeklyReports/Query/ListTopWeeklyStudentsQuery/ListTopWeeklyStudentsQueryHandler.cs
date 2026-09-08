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
        var weekStart = await ResolveBoardWeekStartAsync(dbContext, query.WeekStartDate, cancellationToken);

        var rows = await ScoredReportsForWeek(dbContext, weekStart)
            .Select(x => new
            {
                x.StudentId,
                StudentName = x.Student!.DisplayName,
                StudentGrade = x.Student.Grade,
                PhotoStorageKey = x.Student.ProfilePhotoStorageKey,
                PerformancePercent = x.PerformancePercent!.Value
            })
            .ToListAsync(cancellationToken);

        // One report per subject, so the student's score is the mean of the subjects they were graded in.
        return rows
            .GroupBy(x => x.StudentId)
            .Select(group => new
            {
                Average = group.Average(x => (double)x.PerformancePercent),
                Student = group.First(),
                SubjectCount = group.Count()
            })
            .Where(x => x.Average >= MinPerformancePercent)
            .OrderByDescending(x => x.Average)
            .ThenBy(x => x.Student.StudentName)
            .Select(x => new TopWeeklyStudentDto(
                x.Student.StudentId,
                x.Student.StudentName,
                x.Student.StudentGrade,
                (int)Math.Round(x.Average, MidpointRounding.AwayFromZero),
                x.SubjectCount,
                TopStudentPhotoUrls.Build(x.Student.StudentId, weekStart, x.Student.PhotoStorageKey),
                weekStart))
            .ToList();
    }

    /// <summary>
    /// Public board (no week specified) stays on last week until the current week has any ratings.
    /// An explicit week is always used as-is.
    /// </summary>
    public static async Task<DateOnly> ResolveBoardWeekStartAsync(
        IAppDbContext dbContext,
        DateOnly? requestedWeekStart,
        CancellationToken cancellationToken)
    {
        if (requestedWeekStart.HasValue)
        {
            return requestedWeekStart.Value;
        }

        var currentWeek = StartOfWeek(DateOnly.FromDateTime(DateTime.UtcNow));
        if (await HasScoredReportsAsync(dbContext, currentWeek, cancellationToken))
        {
            return currentWeek;
        }

        return currentWeek.AddDays(-7);
    }

    /// <summary>Guards the anonymous honour-board photo route: only students on the board are served.</summary>
    public static async Task<bool> QualifiesForBoardAsync(
        IAppDbContext dbContext,
        Guid studentId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var scores = await ScoredReportsForWeek(dbContext, weekStart)
            .Where(x => x.StudentId == studentId)
            .Select(x => x.PerformancePercent!.Value)
            .ToListAsync(cancellationToken);

        return scores.Count > 0 && scores.Average() >= MinPerformancePercent;
    }

    private static Task<bool> HasScoredReportsAsync(
        IAppDbContext dbContext,
        DateOnly weekStart,
        CancellationToken cancellationToken) =>
        ScoredReportsForWeek(dbContext, weekStart).AnyAsync(cancellationToken);

    private static IQueryable<Domain.Entities.StudentWeeklyReport> ScoredReportsForWeek(
        IAppDbContext dbContext,
        DateOnly weekStart) =>
        dbContext.StudentWeeklyReports
            .AsNoTracking()
            .Where(x => x.WeekStartDate == weekStart)
            .Where(x => x.PerformancePercent != null)
            .Where(x => x.Student != null && x.Student.IsActive);

    public static DateOnly StartOfWeek(DateOnly date)
    {
        // Monday-based school week (matches teacher UI).
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }
}
