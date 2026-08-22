using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class SaveWeeklyStudyPlanCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SaveWeeklyStudyPlanCommand, WeeklyStudyPlanDto>
{
    public async Task<WeeklyStudyPlanDto> Handle(
        SaveWeeklyStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        StudyPlanAccess.ValidateRange(command.FromDate, command.ToDate);
        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var courseExists = await dbContext.Courses
            .AsNoTracking()
            .AnyAsync(x => x.Id == command.CourseId, cancellationToken);
        if (!courseExists)
        {
            throw new InvalidOperationException("Course not found.");
        }

        var weeks = NormalizeWeeks(command.FromDate, command.ToDate, command.Weeks);
        var now = DateTimeOffset.UtcNow;
        var notes = StudyPlanAccess.Clamp(command.Notes, 1000);
        var tenantId = dbContext.CurrentTenantId;

        Guid planId;
        if (command.Id is Guid existingId && existingId != Guid.Empty)
        {
            var existing = await dbContext.WeeklyStudyPlans
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.Id == existingId && x.TeacherId == command.TeacherId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Study plan not found.");
            planId = existing.Id;
            tenantId = existing.TenantId ?? tenantId;
            await UpdateExistingPlanAsync(command, existing, notes, now, weeks, tenantId, cancellationToken);
        }
        else
        {
            var existing = await dbContext.WeeklyStudyPlans
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TeacherId == command.TeacherId
                        && x.CourseId == command.CourseId
                        && x.FromDate == command.FromDate,
                    cancellationToken);
            if (existing is null)
            {
                planId = await InsertNewPlanAsync(command, notes, now, weeks, tenantId, cancellationToken);
            }
            else
            {
                planId = existing.Id;
                tenantId = existing.TenantId ?? tenantId;
                await UpdateExistingPlanAsync(command, existing, notes, now, weeks, tenantId, cancellationToken);
            }
        }

        var saved = await dbContext.WeeklyStudyPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Include(x => x.Items)
                .ThenInclude(x => x.Topics)
            .FirstAsync(x => x.Id == planId, cancellationToken);

        return StudyPlanAccess.ToDto(saved);
    }

    private async Task UpdateExistingPlanAsync(
        SaveWeeklyStudyPlanCommand command,
        WeeklyStudyPlan existing,
        string notes,
        DateTimeOffset now,
        List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate, int SortOrder, IReadOnlyList<SaveWeeklyStudyPlanTopicDto> Topics)> weeks,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var clash = await dbContext.WeeklyStudyPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Id != existing.Id
                    && x.TeacherId == command.TeacherId
                    && x.CourseId == command.CourseId
                    && x.FromDate == command.FromDate,
                cancellationToken);
        if (clash)
        {
            throw new InvalidOperationException("A study plan already exists for this course and start date.");
        }

        var courseId = command.CourseId;
        var fromDate = command.FromDate;
        var toDate = command.ToDate;
        var updated = await dbContext.WeeklyStudyPlans
            .IgnoreQueryFilters()
            .Where(x => x.Id == existing.Id && x.TeacherId == command.TeacherId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.CourseId, courseId)
                    .SetProperty(x => x.FromDate, fromDate)
                    .SetProperty(x => x.ToDate, toDate)
                    .SetProperty(x => x.Notes, notes)
                    .SetProperty(x => x.UpdatedAtUtc, now)
                    .SetProperty(x => x.TenantId, tenantId),
                cancellationToken);
        if (updated == 0)
        {
            throw new InvalidOperationException("Study plan not found.");
        }

        await ReplaceWeeksAsync(existing.Id, cancellationToken);
        AddWeeks(existing.Id, tenantId, weeks);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> InsertNewPlanAsync(
        SaveWeeklyStudyPlanCommand command,
        string notes,
        DateTimeOffset now,
        List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate, int SortOrder, IReadOnlyList<SaveWeeklyStudyPlanTopicDto> Topics)> weeks,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var plan = new WeeklyStudyPlan
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            FromDate = command.FromDate,
            ToDate = command.ToDate,
            Notes = notes,
            TenantId = tenantId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.WeeklyStudyPlans.Add(plan);
        AddWeeks(plan.Id, tenantId, weeks);
        await dbContext.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    private void AddWeeks(
        Guid planId,
        string? tenantId,
        List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate, int SortOrder, IReadOnlyList<SaveWeeklyStudyPlanTopicDto> Topics)> weeks)
    {
        foreach (var week in weeks)
        {
            var item = new WeeklyStudyPlanItem
            {
                Id = Guid.NewGuid(),
                WeeklyStudyPlanId = planId,
                WeekNumber = week.WeekNumber,
                FromDate = week.FromDate,
                ToDate = week.ToDate,
                SortOrder = week.SortOrder,
                TenantId = tenantId
            };
            var topicOrder = 0;
            foreach (var topic in week.Topics)
            {
                var title = StudyPlanAccess.Clamp(topic.Title, StudyPlanAccess.TopicTitleMax);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                item.Topics.Add(new WeeklyStudyPlanTopic
                {
                    Id = Guid.NewGuid(),
                    WeeklyStudyPlanItemId = item.Id,
                    Title = title,
                    Highlight = topic.Highlight,
                    SortOrder = topicOrder++,
                    TenantId = tenantId
                });
            }

            dbContext.WeeklyStudyPlanItems.Add(item);
        }
    }

    private async Task ReplaceWeeksAsync(Guid planId, CancellationToken cancellationToken)
    {
        var itemIds = await dbContext.WeeklyStudyPlanItems
            .IgnoreQueryFilters()
            .Where(x => x.WeeklyStudyPlanId == planId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (itemIds.Count == 0)
        {
            return;
        }

        await dbContext.WeeklyStudyPlanTopics
            .IgnoreQueryFilters()
            .Where(x => itemIds.Contains(x.WeeklyStudyPlanItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.WeeklyStudyPlanItems
            .IgnoreQueryFilters()
            .Where(x => x.WeeklyStudyPlanId == planId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate, int SortOrder, IReadOnlyList<SaveWeeklyStudyPlanTopicDto> Topics)>
        NormalizeWeeks(DateOnly fromDate, DateOnly toDate, IReadOnlyList<SaveWeeklyStudyPlanWeekDto> incoming)
    {
        var byNumber = incoming
            .Where(x => x.WeekNumber > 0)
            .GroupBy(x => x.WeekNumber)
            .ToDictionary(g => g.Key, g => g.Last());

        var generated = StudyPlanAccess.BuildSchoolWeeks(fromDate, toDate);
        var result = new List<(int, DateOnly, DateOnly, int, IReadOnlyList<SaveWeeklyStudyPlanTopicDto>)>();
        foreach (var week in generated)
        {
            byNumber.TryGetValue(week.WeekNumber, out var match);
            var weekFrom = match is { FromDate: var f } && f != default ? f : week.FromDate;
            var weekTo = match is { ToDate: var t } && t != default ? t : week.ToDate;
            if (weekTo < weekFrom)
            {
                weekTo = weekFrom;
            }

            result.Add((
                week.WeekNumber,
                weekFrom,
                weekTo,
                week.WeekNumber - 1,
                CombineTopics(match?.Topics)));
        }

        return result;
    }

    private static IReadOnlyList<SaveWeeklyStudyPlanTopicDto> CombineTopics(
        IReadOnlyList<SaveWeeklyStudyPlanTopicDto>? topics)
    {
        var list = topics ?? [];
        var titles = list
            .Select(topic => StudyPlanAccess.Clamp(topic.Title, StudyPlanAccess.TopicTitleMax).Trim())
            .Where(title => title.Length > 0)
            .ToList();
        if (titles.Count == 0)
        {
            return [];
        }

        return
        [
            new SaveWeeklyStudyPlanTopicDto(
                StudyPlanAccess.Clamp(string.Join("\n", titles), StudyPlanAccess.TopicTitleMax),
                list.Any(topic => topic.Highlight))
        ];
    }
}
