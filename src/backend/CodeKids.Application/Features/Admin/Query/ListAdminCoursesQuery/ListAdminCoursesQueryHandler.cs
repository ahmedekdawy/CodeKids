using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class ListAdminCoursesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListAdminCoursesQuery, PagedCoursesResultDto>
{
    public async Task<PagedCoursesResultDto> Handle(
        ListAdminCoursesQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);
        var search = (query.TitleSearch ?? string.Empty).Trim().ToLowerInvariant();
        var sortKey = (query.SortKey ?? "title").Trim();
        var sortDir = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var baseQuery = dbContext.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(c => c.Title.ToLower().Contains(search));
        }

        if (query.StageId is int stageId)
        {
            baseQuery = baseQuery.Where(c =>
                c.StageId == stageId ||
                (c.Grade != null && (
                    ((c.Grade == -1 || c.Grade == 0) && stageId == 0) ||
                    (c.Grade >= 1 && c.Grade <= 6 && stageId == 1) ||
                    (c.Grade >= 7 && c.Grade <= 9 && stageId == 2) ||
                    (c.Grade >= 10 && c.Grade <= 12 && stageId == 3))));
        }

        if (query.Grade is int grade)
        {
            var gradeStage = GradeToStage(grade);
            baseQuery = baseQuery.Where(c =>
                c.Grade == grade ||
                (c.Grade == null && (c.StageId == null || c.StageId == gradeStage)));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = ApplySort(baseQuery, sortKey, sortDir);
        var courses = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = courses.Select(CreateCourseCommandHandler.ToSummary).ToList();
        return new PagedCoursesResultDto(items, totalCount, page, pageSize);
    }

    private static int? GradeToStage(int grade)
    {
        if (grade is -1 or 0) return 0;
        if (grade is >= 1 and <= 6) return 1;
        if (grade is >= 7 and <= 9) return 2;
        if (grade is >= 10 and <= 12) return 3;
        return null;
    }

    private static IQueryable<Domain.Entities.Course> ApplySort(
        IQueryable<Domain.Entities.Course> query,
        string sortKey,
        string sortDir)
    {
        var desc = sortDir == "desc";
        return sortKey switch
        {
            "theme" => desc
                ? query.OrderByDescending(c => c.Theme).ThenBy(c => c.Title)
                : query.OrderBy(c => c.Theme).ThenBy(c => c.Title),
            "term" => desc
                ? query.OrderByDescending(c => c.TermId).ThenBy(c => c.Title)
                : query.OrderBy(c => c.TermId).ThenBy(c => c.Title),
            "grade" => desc
                ? query.OrderByDescending(c => c.Grade).ThenBy(c => c.Title)
                : query.OrderBy(c => c.Grade).ThenBy(c => c.Title),
            "schoolType" => desc
                ? query.OrderByDescending(c => c.SchoolType).ThenBy(c => c.Title)
                : query.OrderBy(c => c.SchoolType).ThenBy(c => c.Title),
            "ageMin" => desc
                ? query.OrderByDescending(c => c.AgeMin).ThenBy(c => c.Title)
                : query.OrderBy(c => c.AgeMin).ThenBy(c => c.Title),
            "sortOrder" => desc
                ? query.OrderByDescending(c => c.SortOrder).ThenBy(c => c.Title)
                : query.OrderBy(c => c.SortOrder).ThenBy(c => c.Title),
            _ => desc
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title)
        };
    }
}
