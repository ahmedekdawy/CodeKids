using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed class ListTeacherSessionAttendanceQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTeacherSessionAttendanceQuery, IReadOnlyList<TeacherSessionAttendanceDto>>
{
    public async Task<IReadOnlyList<TeacherSessionAttendanceDto>> Handle(
        ListTeacherSessionAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            rows = rows.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.CourseGrade.HasValue)
        {
            rows = rows.Where(x => x.Course != null && x.Course.Grade == query.CourseGrade.Value);
        }

        if (query.SessionDate.HasValue)
        {
            rows = rows.Where(x => x.SessionDate == query.SessionDate.Value);
        }
        else
        {
            if (query.FromDate.HasValue)
            {
                rows = rows.Where(x => x.SessionDate >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                rows = rows.Where(x => x.SessionDate <= query.ToDate.Value);
            }
        }

        return (await rows
            .OrderByDescending(x => x.SessionDate)
            .ThenBy(x => x.Teacher!.DisplayName)
            .ThenBy(x => x.Course!.Grade)
            .ThenBy(x => x.Course!.Title)
            .ToListAsync(cancellationToken))
            .Select(TeacherSessionAttendanceValidators.ToDto)
            .ToList();
    }
}
