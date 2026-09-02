using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class ListClassroomEnrollmentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListClassroomEnrollmentsQuery, PagedClassroomEnrollmentsResultDto>
{
    public async Task<PagedClassroomEnrollmentsResultDto> Handle(
        ListClassroomEnrollmentsQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);
        var search = (query.StudentSearch ?? string.Empty).Trim().ToLowerInvariant();
        var sortKey = (query.SortKey ?? "classroomName").Trim();
        var sortDir = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var baseQuery = dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(cs => cs.Student != null && cs.Classroom != null);

        if (query.ClassroomId is Guid classroomId)
        {
            baseQuery = baseQuery.Where(cs => cs.ClassroomId == classroomId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(cs =>
                cs.Student!.DisplayName.ToLower().Contains(search) ||
                cs.Student!.Email.ToLower().Contains(search));
        }

        if (query.CourseId is Guid courseId)
        {
            baseQuery = baseQuery.Where(cs =>
                dbContext.StudentCourseEnrollments.Any(e =>
                    e.StudentId == cs.StudentId &&
                    e.ClassroomId == cs.ClassroomId &&
                    e.CourseId == courseId) ||
                (!dbContext.StudentCourseEnrollments.Any(e =>
                    e.StudentId == cs.StudentId &&
                    e.ClassroomId == cs.ClassroomId) &&
                 (dbContext.ClassroomCourses.Any(cc =>
                      cc.ClassroomId == cs.ClassroomId &&
                      cc.CourseId == courseId) ||
                  dbContext.Classrooms.Any(c =>
                      c.Id == cs.ClassroomId &&
                      c.CourseId == courseId))));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = ApplySort(baseQuery, sortKey, sortDir);
        var pageRows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(cs => new
            {
                cs.Id,
                cs.ClassroomId,
                ClassroomName = cs.Classroom!.Name,
                cs.StudentId,
                StudentName = cs.Student!.DisplayName,
                StudentEmail = cs.Student.Email
            })
            .ToListAsync(cancellationToken);

        if (pageRows.Count == 0)
        {
            return new PagedClassroomEnrollmentsResultDto([], totalCount, page, pageSize);
        }

        var classroomIds = pageRows.Select(x => x.ClassroomId).Distinct().ToList();
        var studentIds = pageRows.Select(x => x.StudentId).Distinct().ToList();

        var explicitEnrollments = await dbContext.StudentCourseEnrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => classroomIds.Contains(e.ClassroomId) && studentIds.Contains(e.StudentId))
            .ToListAsync(cancellationToken);

        var enrollmentLookup = explicitEnrollments
            .GroupBy(e => (e.ClassroomId, e.StudentId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Course?.Title ?? string.Empty).ToList());

        var items = pageRows.Select(row =>
        {
            var key = (row.ClassroomId, row.StudentId);
            var enrolled = enrollmentLookup.TryGetValue(key, out var list) ? list : [];
            return new ClassroomEnrollmentListItemDto(
                row.ClassroomId,
                row.ClassroomName,
                row.StudentId,
                row.StudentName,
                row.StudentEmail,
                enrolled.Select(e => e.CourseId).ToList(),
                enrolled
                    .Select(e => e.Course?.Title)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Cast<string>()
                    .ToList());
        }).ToList();

        return new PagedClassroomEnrollmentsResultDto(items, totalCount, page, pageSize);
    }

    private IQueryable<Domain.Entities.ClassroomStudent> ApplySort(
        IQueryable<Domain.Entities.ClassroomStudent> query,
        string sortKey,
        string sortDir)
    {
        var desc = sortDir == "desc";
        return sortKey switch
        {
            "studentName" => desc
                ? query.OrderByDescending(cs => cs.Student!.DisplayName).ThenBy(cs => cs.Classroom!.Name)
                : query.OrderBy(cs => cs.Student!.DisplayName).ThenBy(cs => cs.Classroom!.Name),
            "studentEmail" => desc
                ? query.OrderByDescending(cs => cs.Student!.Email).ThenBy(cs => cs.Classroom!.Name)
                : query.OrderBy(cs => cs.Student!.Email).ThenBy(cs => cs.Classroom!.Name),
            "coursesLabel" => desc
                ? query.OrderByDescending(cs =>
                    dbContext.StudentCourseEnrollments
                        .Where(e => e.StudentId == cs.StudentId && e.ClassroomId == cs.ClassroomId)
                        .Join(dbContext.Courses, e => e.CourseId, c => c.Id, (e, c) => c.Title)
                        .OrderBy(t => t)
                        .FirstOrDefault() ?? string.Empty)
                    .ThenBy(cs => cs.Classroom!.Name)
                : query.OrderBy(cs =>
                    dbContext.StudentCourseEnrollments
                        .Where(e => e.StudentId == cs.StudentId && e.ClassroomId == cs.ClassroomId)
                        .Join(dbContext.Courses, e => e.CourseId, c => c.Id, (e, c) => c.Title)
                        .OrderBy(t => t)
                        .FirstOrDefault() ?? string.Empty)
                    .ThenBy(cs => cs.Classroom!.Name),
            _ => desc
                ? query.OrderByDescending(cs => cs.Classroom!.Name).ThenBy(cs => cs.Student!.DisplayName)
                : query.OrderBy(cs => cs.Classroom!.Name).ThenBy(cs => cs.Student!.DisplayName)
        };
    }
}
