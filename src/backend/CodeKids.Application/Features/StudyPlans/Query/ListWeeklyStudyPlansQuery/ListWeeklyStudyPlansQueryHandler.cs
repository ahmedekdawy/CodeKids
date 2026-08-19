using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class ListWeeklyStudyPlansQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListWeeklyStudyPlansQuery, IReadOnlyList<WeeklyStudyPlanDto>>
{
    public async Task<IReadOnlyList<WeeklyStudyPlanDto>> Handle(
        ListWeeklyStudyPlansQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.WeeklyStudyPlans
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Items)
                .ThenInclude(x => x.Topics)
            .Where(x => x.TeacherId == query.TeacherId)
            .AsQueryable();

        if (query.CourseId.HasValue)
        {
            rows = rows.Where(x => x.CourseId == query.CourseId.Value);
        }

        if (query.FromDate.HasValue)
        {
            rows = rows.Where(x => x.ToDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            rows = rows.Where(x => x.FromDate <= query.ToDate.Value);
        }

        var plans = await rows
            .OrderByDescending(x => x.FromDate)
            .ThenBy(x => x.Course != null ? x.Course.Title : string.Empty)
            .ToListAsync(cancellationToken);

        return plans.Select(StudyPlanAccess.ToDto).ToList();
    }
}
