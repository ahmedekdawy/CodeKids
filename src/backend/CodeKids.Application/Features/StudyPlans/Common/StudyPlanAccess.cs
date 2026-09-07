using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

internal static class StudyPlanAccess
{
    internal const int MaxWeeks = 20;
    internal const int TopicTitleMax = 1000;
    internal const int PromptMax = 2000;

    internal static async Task EnsureTeacherOwnsCourseAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var owns = await dbContext.ClassroomCourses
            .AsNoTracking()
            .AnyAsync(x => x.TeacherId == teacherId && x.CourseId == courseId, cancellationToken);
        if (!owns)
        {
            throw new InvalidOperationException("Course is not assigned to this teacher.");
        }
    }

    internal static void ValidateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate == default || toDate == default)
        {
            throw new InvalidOperationException("From and to dates are required.");
        }

        if (toDate < fromDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var weeks = BuildSchoolWeeks(fromDate, toDate);
        if (weeks.Count == 0)
        {
            throw new InvalidOperationException("From and to dates are required.");
        }

        if (weeks.Count > MaxWeeks)
        {
            throw new InvalidOperationException("Study plan cannot exceed 20 weeks.");
        }
    }

    internal static List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate)> BuildSchoolWeeks(
        DateOnly fromDate,
        DateOnly toDate)
    {
        var weeks = new List<(int, DateOnly, DateOnly)>();
        var sunday = fromDate.AddDays(-(int)fromDate.DayOfWeek);
        var weekNumber = 1;
        for (var weekStart = sunday;
             weekStart <= toDate && weekNumber <= MaxWeeks;
             weekStart = weekStart.AddDays(7), weekNumber++)
        {
            var clippedFrom = weekStart < fromDate ? fromDate : weekStart;
            var clippedTo = weekStart.AddDays(4);
            if (clippedTo > toDate)
            {
                clippedTo = toDate;
            }

            if (clippedFrom > clippedTo)
            {
                continue;
            }

            weeks.Add((weeks.Count + 1, clippedFrom, clippedTo));
        }

        return weeks;
    }

    internal static string Clamp(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    internal static WeeklyStudyPlanDto ToDto(WeeklyStudyPlan plan) =>
        new(
            plan.Id,
            plan.TeacherId,
            plan.Teacher?.DisplayName ?? string.Empty,
            plan.CourseId,
            plan.Course?.Title ?? string.Empty,
            plan.Course?.Grade,
            plan.Course?.StageId,
            plan.Course?.TermId?.ToString(),
            plan.FromDate,
            plan.ToDate,
            plan.Notes,
            plan.Items
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.WeekNumber)
                .Select(x => new WeeklyStudyPlanWeekDto(
                    x.Id,
                    x.WeekNumber,
                    x.FromDate,
                    x.ToDate,
                    x.SortOrder,
                    x.Topics
                        .OrderBy(t => t.SortOrder)
                        .Select(t => new WeeklyStudyPlanTopicDto(
                            t.Id,
                            t.Title,
                            t.Highlight,
                            t.SortOrder))
                        .ToList()))
                .ToList());
}
