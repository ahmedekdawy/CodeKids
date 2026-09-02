using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed class ListStudentClassroomAttendanceQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListStudentClassroomAttendanceQuery, PagedStudentClassroomAttendanceResultDto>
{
    public async Task<PagedStudentClassroomAttendanceResultDto> Handle(
        ListStudentClassroomAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        StudentClassroomAttendanceAccess.ValidateDateRange(query.FromDate, query.ToDate);

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);
        var search = (query.StudentSearch ?? string.Empty).Trim().ToLowerInvariant();
        var sortKey = (query.SortKey ?? "attendanceDate").Trim();
        var sortDir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (!isTeacher && !isAdmin)
        {
            return new PagedStudentClassroomAttendanceResultDto([], 0, page, pageSize);
        }

        var baseQuery = dbContext.StudentClassroomAttendances
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Classroom)
            .Include(x => x.RecordedByTeacher)
            .AsQueryable();

        if (isTeacher)
        {
            baseQuery = baseQuery.Where(x =>
                dbContext.ClassroomCourses.Any(cc =>
                    cc.ClassroomId == x.ClassroomId && cc.TeacherId == query.ViewerUserId));
        }

        if (query.ClassroomId is Guid classroomId)
        {
            baseQuery = baseQuery.Where(x => x.ClassroomId == classroomId);
        }

        if (query.GradeId is int gradeId)
        {
            baseQuery = baseQuery.Where(x => x.Student!.Grade == gradeId);
        }

        if (query.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.AttendanceDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.AttendanceDate <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(x =>
                x.Student!.DisplayName.ToLower().Contains(search) ||
                x.Student!.Email.ToLower().Contains(search));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = ApplySort(baseQuery, sortKey, sortDir);
        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(StudentClassroomAttendanceAccess.ToDto).ToList();
        return new PagedStudentClassroomAttendanceResultDto(items, totalCount, page, pageSize);
    }

    private static IQueryable<Domain.Entities.StudentClassroomAttendance> ApplySort(
        IQueryable<Domain.Entities.StudentClassroomAttendance> query,
        string sortKey,
        string sortDir)
    {
        var asc = sortDir == "asc";
        return sortKey switch
        {
            "studentName" => asc
                ? query.OrderBy(x => x.Student!.DisplayName).ThenByDescending(x => x.AttendanceDate)
                : query.OrderByDescending(x => x.Student!.DisplayName).ThenByDescending(x => x.AttendanceDate),
            "studentGradeId" => asc
                ? query.OrderBy(x => x.Student!.Grade).ThenBy(x => x.Student!.DisplayName)
                : query.OrderByDescending(x => x.Student!.Grade).ThenBy(x => x.Student!.DisplayName),
            "classroomName" => asc
                ? query.OrderBy(x => x.Classroom!.Name).ThenByDescending(x => x.AttendanceDate)
                : query.OrderByDescending(x => x.Classroom!.Name).ThenByDescending(x => x.AttendanceDate),
            "status" => asc
                ? query.OrderBy(x => x.Status).ThenByDescending(x => x.AttendanceDate)
                : query.OrderByDescending(x => x.Status).ThenByDescending(x => x.AttendanceDate),
            "attendanceDate" => asc
                ? query.OrderBy(x => x.AttendanceDate).ThenBy(x => x.Student!.DisplayName)
                : query.OrderByDescending(x => x.AttendanceDate).ThenBy(x => x.Student!.DisplayName),
            _ => asc
                ? query.OrderBy(x => x.AttendanceDate).ThenBy(x => x.Student!.DisplayName)
                : query.OrderByDescending(x => x.AttendanceDate).ThenBy(x => x.Student!.DisplayName)
        };
    }
}
