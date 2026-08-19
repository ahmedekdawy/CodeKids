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

        WeeklyStudyPlan? plan;
        if (command.Id is Guid existingId && existingId != Guid.Empty)
        {
            plan = await dbContext.WeeklyStudyPlans
                .Include(x => x.Items)
                    .ThenInclude(x => x.Topics)
                .FirstOrDefaultAsync(
                    x => x.Id == existingId && x.TeacherId == command.TeacherId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Study plan not found.");
        }
        else
        {
            plan = await dbContext.WeeklyStudyPlans
                .Include(x => x.Items)
                    .ThenInclude(x => x.Topics)
                .FirstOrDefaultAsync(
                    x => x.TeacherId == command.TeacherId
                        && x.CourseId == command.CourseId
                        && x.FromDate == command.FromDate,
                    cancellationToken);
        }

        if (plan is null)
        {
            plan = new WeeklyStudyPlan
            {
                Id = Guid.NewGuid(),
                TeacherId = command.TeacherId,
                CourseId = command.CourseId,
                FromDate = command.FromDate,
                ToDate = command.ToDate,
                Notes = StudyPlanAccess.Clamp(command.Notes, 1000),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.WeeklyStudyPlans.Add(plan);
        }
        else
        {
            var clash = await dbContext.WeeklyStudyPlans
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id != plan.Id
                        && x.TeacherId == command.TeacherId
                        && x.CourseId == command.CourseId
                        && x.FromDate == command.FromDate,
                    cancellationToken);
            if (clash)
            {
                throw new InvalidOperationException("A study plan already exists for this course and start date.");
            }

            plan.CourseId = command.CourseId;
            plan.FromDate = command.FromDate;
            plan.ToDate = command.ToDate;
            plan.Notes = StudyPlanAccess.Clamp(command.Notes, 1000);
            plan.UpdatedAtUtc = now;
            dbContext.WeeklyStudyPlanItems.RemoveRange(plan.Items);
            plan.Items.Clear();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var week in weeks)
        {
            var item = new WeeklyStudyPlanItem
            {
                Id = Guid.NewGuid(),
                WeeklyStudyPlanId = plan.Id,
                WeekNumber = week.WeekNumber,
                FromDate = week.FromDate,
                ToDate = week.ToDate,
                SortOrder = week.SortOrder
            };
            var topicOrder = 0;
            foreach (var topic in week.Topics)
            {
                var title = StudyPlanAccess.Clamp(topic.Title, 300);
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
                    SortOrder = topicOrder++
                });
            }

            plan.Items.Add(item);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.WeeklyStudyPlans
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Items)
                .ThenInclude(x => x.Topics)
            .FirstAsync(x => x.Id == plan.Id, cancellationToken);

        return StudyPlanAccess.ToDto(saved);
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
                match?.Topics ?? []));
        }

        return result;
    }
}
