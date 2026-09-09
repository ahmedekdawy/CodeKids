using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Badges;

public static class BadgeAwarder
{
    public static async Task AwardEligibleAsync(IAppDbContext dbContext, User user, CancellationToken cancellationToken)
    {
        var completedSteps = await dbContext.StudentProgress
            .CountAsync(x => x.UserId == user.Id && x.IsCompleted, cancellationToken);

        var ownedBadgeIds = await dbContext.UserBadges
            .Where(x => x.UserId == user.Id)
            .Select(x => x.BadgeId)
            .ToListAsync(cancellationToken);

        var unowned = await dbContext.Badges
            .Where(x => !ownedBadgeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var eligible = new List<Badge>();
        foreach (var badge in unowned)
        {
            if (AchievementBadgeCodes.All.Contains(badge.Code))
            {
                if (await MeetsAchievementAsync(dbContext, user.Id, badge.Code, cancellationToken))
                {
                    eligible.Add(badge);
                }

                continue;
            }

            if (user.TotalXp >= badge.RequiredXp && completedSteps >= badge.RequiredSteps)
            {
                eligible.Add(badge);
            }
        }

        foreach (var badge in eligible)
        {
            dbContext.UserBadges.Add(new UserBadge
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BadgeId = badge.Id,
                AwardedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (eligible.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static Task<bool> MeetsAchievementAsync(
        IAppDbContext dbContext,
        Guid userId,
        string code,
        CancellationToken cancellationToken) =>
        code switch
        {
            AchievementBadgeCodes.WeeklyStar => dbContext.StudentWeeklyReports.AnyAsync(
                x => x.StudentId == userId && x.PerformancePercent != null && x.PerformancePercent > 90,
                cancellationToken),
            AchievementBadgeCodes.AssignmentAce => dbContext.AssignmentSubmissions.AnyAsync(
                x => x.StudentId == userId
                     && x.Status == AssignmentSubmissionStatus.Graded
                     && x.Score != null
                     && x.MaxScore != null
                     && x.MaxScore > 0
                     && x.Score * 100 >= 98 * x.MaxScore,
                cancellationToken),
            AchievementBadgeCodes.ExamStar => dbContext.ExamAttempts.AnyAsync(
                x => x.StudentId == userId
                     && x.Status == ExamAttemptStatus.Graded
                     && x.Score != null
                     && x.MaxScore != null
                     && x.MaxScore > 0
                     && x.Score * 100 > 95 * x.MaxScore,
                cancellationToken),
            AchievementBadgeCodes.QuizAce => dbContext.QuizAttempts.AnyAsync(
                x => x.UserId == userId
                     && x.TotalQuestions > 0
                     && x.Score * 100 >= 98 * x.TotalQuestions,
                cancellationToken),
            _ => Task.FromResult(false)
        };
}
