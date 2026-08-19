using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
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
            .Include(x => x.Teacher)
            .Include(x => x.Items)
                .ThenInclude(x => x.Topics)
            .AsQueryable();

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isParent = string.Equals(query.ViewerRole, nameof(UserRole.Parent), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            rows = rows.Where(x => x.TeacherId == query.ViewerUserId);
        }
        else if (isAdmin)
        {
            if (query.TeacherId.HasValue)
            {
                rows = rows.Where(x => x.TeacherId == query.TeacherId.Value);
            }
        }
        else if (isStudent)
        {
            var courseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                dbContext, query.ViewerUserId, cancellationToken);
            if (courseIds.Count == 0)
            {
                return [];
            }

            rows = rows.Where(x => courseIds.Contains(x.CourseId));
        }
        else if (isParent)
        {
            var childIds = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.ParentId == query.ViewerUserId && x.Role == UserRole.Student)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (query.StudentId.HasValue)
            {
                if (!childIds.Contains(query.StudentId.Value))
                {
                    throw new InvalidOperationException("This student is not linked to your account.");
                }

                childIds = [query.StudentId.Value];
            }

            if (childIds.Count == 0)
            {
                return [];
            }

            var courseIds = new HashSet<Guid>();
            foreach (var childId in childIds)
            {
                courseIds.UnionWith(await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                    dbContext, childId, cancellationToken));
            }

            if (courseIds.Count == 0)
            {
                return [];
            }

            rows = rows.Where(x => courseIds.Contains(x.CourseId));
        }
        else
        {
            return [];
        }

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
            .ThenBy(x => x.Teacher!.DisplayName)
            .ThenBy(x => x.Course!.Title)
            .ToListAsync(cancellationToken);

        return plans.Select(StudyPlanAccess.ToDto).ToList();
    }
}
