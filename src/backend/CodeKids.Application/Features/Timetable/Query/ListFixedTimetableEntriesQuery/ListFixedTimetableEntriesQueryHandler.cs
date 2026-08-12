using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public sealed class ListFixedTimetableEntriesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListFixedTimetableEntriesQuery, IReadOnlyList<FixedTimetableEntryDto>>
{
    public async Task<IReadOnlyList<FixedTimetableEntryDto>> Handle(
        ListFixedTimetableEntriesQuery query,
        CancellationToken cancellationToken)
    {
        var entries = dbContext.FixedTimetableEntries
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            entries = entries.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.CourseGrade.HasValue)
        {
            entries = entries.Where(x => x.Course != null && x.Course.Grade == query.CourseGrade.Value);
        }

        if (query.Period.HasValue)
        {
            entries = entries.Where(x => x.Period == query.Period.Value);
        }

        return (await entries
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.Period)
            .ThenBy(x => x.SessionNumber)
            .ThenBy(x => x.Teacher!.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(FixedTimetableValidators.ToDto)
            .ToList();
    }
}
