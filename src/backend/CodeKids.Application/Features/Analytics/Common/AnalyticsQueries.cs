using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public static class AnalyticsQueries
{
    public static async Task<IReadOnlyList<LessonWeaknessDto>> GetWeakLessonsForStudent(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ExamAnswers
            .AsNoTracking()
            .Include(x => x.Attempt)
            .Include(x => x.Question)
                .ThenInclude(q => q!.Lesson)
            .Where(x => x.Attempt!.StudentId == studentId
                        && x.Question!.LessonId != null
                        && x.IsCorrect != null)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => new { LessonId = x.Question!.LessonId!.Value, Title = x.Question.Lesson?.Title ?? "Lesson" })
            .Select(g =>
            {
                var total = g.Count();
                var wrong = g.Count(x => x.IsCorrect == false);
                var accuracy = total == 0 ? 100 : (int)Math.Round((total - wrong) * 100.0 / total);
                return new LessonWeaknessDto(g.Key.LessonId, g.Key.Title, wrong, total, accuracy);
            })
            .Where(x => x.WrongAnswers > 0)
            .OrderBy(x => x.AccuracyPercent)
            .ThenByDescending(x => x.WrongAnswers)
            .Take(10)
            .ToList();
    }

    public static async Task<IReadOnlyList<LessonWeaknessDto>> GetWeakLessonsForClassroom(
        IAppDbContext dbContext,
        IReadOnlyList<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.ExamAnswers
            .AsNoTracking()
            .Include(x => x.Attempt)
            .Include(x => x.Question)
                .ThenInclude(q => q!.Lesson)
            .Where(x => studentIds.Contains(x.Attempt!.StudentId)
                        && x.Question!.LessonId != null
                        && x.IsCorrect != null)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => new { LessonId = x.Question!.LessonId!.Value, Title = x.Question.Lesson?.Title ?? "Lesson" })
            .Select(g =>
            {
                var total = g.Count();
                var wrong = g.Count(x => x.IsCorrect == false);
                var accuracy = total == 0 ? 100 : (int)Math.Round((total - wrong) * 100.0 / total);
                return new LessonWeaknessDto(g.Key.LessonId, g.Key.Title, wrong, total, accuracy);
            })
            .Where(x => x.WrongAnswers > 0 && x.AccuracyPercent < 70)
            .OrderBy(x => x.AccuracyPercent)
            .Take(10)
            .ToList();
    }

    public static async Task<string> BuildStudentDigestAsync(
        IAppDbContext dbContext,
        User student,
        CancellationToken cancellationToken)
    {
        var level = StudentLevelCalculator.FromXp(student.TotalXp);
        var completedToday = await dbContext.StudentProgress.CountAsync(
            x => x.UserId == student.Id
                 && x.IsCompleted
                 && x.CompletedAtUtc.Date == DateTime.UtcNow.Date,
            cancellationToken);

        var watchToday = await dbContext.VideoWatchSessions
            .Where(x => x.StudentId == student.Id && x.LastEventAtUtc.Date == DateTime.UtcNow.Date)
            .SumAsync(x => (int?)x.ActualWatchSeconds, cancellationToken) ?? 0;

        var weaknesses = await GetWeakLessonsForStudent(dbContext, student.Id, cancellationToken);
        var weakText = weaknesses.Count == 0
            ? "No weak lessons flagged today."
            : "Focus: " + string.Join(", ", weaknesses.Take(2).Select(w => w.LessonTitle));

        return
            $"CodeKids daily report for {student.DisplayName}\n" +
            $"Level {level.LevelNumber} ({level.Name}) · {student.TotalXp} XP\n" +
            $"Today: {completedToday} steps · {watchToday}s video watched\n" +
            weakText;
    }
}
