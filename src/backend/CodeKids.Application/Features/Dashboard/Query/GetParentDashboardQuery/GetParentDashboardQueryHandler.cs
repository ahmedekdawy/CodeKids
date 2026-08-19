using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed class GetParentDashboardQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    public async Task<ParentDashboardDto> Handle(GetParentDashboardQuery query, CancellationToken cancellationToken)
    {
        var parent = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ParentId && x.Role == UserRole.Parent, cancellationToken)
            ?? throw new InvalidOperationException("Parent account not found.");

        var children = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Badges)
                .ThenInclude(x => x.Badge)
            .Where(x => x.ParentId == parent.Id && x.Role == UserRole.Student)
            .ToListAsync(cancellationToken);

        var childIds = children.Select(x => x.Id).ToList();
        var grades = await StudentGradeResolver.ResolveAsync(
            dbContext,
            children.Select(x => (x.Id, x.Grade)),
            cancellationToken);

        var progressCounts = await dbContext.StudentProgress
            .Where(x => childIds.Contains(x.UserId) && x.IsCompleted)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var quizCounts = await dbContext.QuizAttempts
            .Where(x => childIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var latestEvaluations = childIds.Count == 0
            ? new Dictionary<Guid, ChildEvaluationSummaryDto>()
            : (await dbContext.StudentWeeklyReports
                .AsNoTracking()
                .Include(x => x.Teacher)
                .Where(x => childIds.Contains(x.StudentId))
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g
                            .OrderByDescending(x => x.WeekStartDate)
                            .ThenByDescending(x => x.UpdatedAtUtc)
                            .First();
                        return new ChildEvaluationSummaryDto(
                            latest.WeekStartDate,
                            latest.Teacher?.DisplayName,
                            latest.PerformancePercent,
                            latest.AttendancePercent,
                            latest.HomeworkPercent,
                            latest.InteractionDuringSession,
                            latest.OpenCamera);
                    });

        return new ParentDashboardDto(
            parent.Id,
            parent.DisplayName,
            parent.Email,
            parent.MobilePhone,
            children.Select(child => new ChildProgressDto(
                child.Id,
                child.DisplayName,
                child.Email,
                child.MobilePhone,
                grades.GetValueOrDefault(child.Id) ?? child.Grade,
                child.TotalXp,
                progressCounts.GetValueOrDefault(child.Id),
                quizCounts.GetValueOrDefault(child.Id),
                child.AvatarId,
                child.Badges.Select(x => x.Badge!.Name).ToList(),
                latestEvaluations.GetValueOrDefault(child.Id))).ToList());
    }
}
