using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class ListAdminWeeklyStudyPlansQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListAdminWeeklyStudyPlansQuery, PagedWeeklyStudyPlansResultDto>
{
    public async Task<PagedWeeklyStudyPlansResultDto> Handle(
        ListAdminWeeklyStudyPlansQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);
        var sortKey = (query.SortKey ?? "fromDate").Trim();
        var sortDir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var baseQuery = dbContext.WeeklyStudyPlans
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Include(x => x.Items)
                .ThenInclude(x => x.Topics)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.CourseId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.CourseId == query.CourseId.Value);
        }

        if (query.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ToDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.FromDate <= query.ToDate.Value);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = ApplySort(baseQuery, sortKey, sortDir);
        var plans = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = plans.Select(StudyPlanAccess.ToDto).ToList();
        return new PagedWeeklyStudyPlansResultDto(items, totalCount, page, pageSize);
    }

    private static IQueryable<Domain.Entities.WeeklyStudyPlan> ApplySort(
        IQueryable<Domain.Entities.WeeklyStudyPlan> query,
        string sortKey,
        string sortDir)
    {
        var asc = sortDir == "asc";
        return sortKey switch
        {
            "teacherName" => asc
                ? query.OrderBy(x => x.Teacher!.DisplayName).ThenByDescending(x => x.FromDate)
                : query.OrderByDescending(x => x.Teacher!.DisplayName).ThenByDescending(x => x.FromDate),
            "courseName" => asc
                ? query.OrderBy(x => x.Course!.Title).ThenByDescending(x => x.FromDate)
                : query.OrderByDescending(x => x.Course!.Title).ThenByDescending(x => x.FromDate),
            "toDate" => asc
                ? query.OrderBy(x => x.ToDate).ThenBy(x => x.Teacher!.DisplayName)
                : query.OrderByDescending(x => x.ToDate).ThenBy(x => x.Teacher!.DisplayName),
            "weeksCount" => asc
                ? query.OrderBy(x => x.Items.Count).ThenByDescending(x => x.FromDate)
                : query.OrderByDescending(x => x.Items.Count).ThenByDescending(x => x.FromDate),
            "fromDate" => asc
                ? query.OrderBy(x => x.FromDate).ThenBy(x => x.Teacher!.DisplayName).ThenBy(x => x.Course!.Title)
                : query.OrderByDescending(x => x.FromDate).ThenBy(x => x.Teacher!.DisplayName).ThenBy(x => x.Course!.Title),
            _ => asc
                ? query.OrderBy(x => x.FromDate).ThenBy(x => x.Teacher!.DisplayName).ThenBy(x => x.Course!.Title)
                : query.OrderByDescending(x => x.FromDate).ThenBy(x => x.Teacher!.DisplayName).ThenBy(x => x.Course!.Title)
        };
    }
}
