using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public sealed record ListFixedTimetableEntriesQuery(
    Guid? TeacherId = null,
    int? CourseGrade = null,
    TimetablePeriod? Period = null) : IQuery<IReadOnlyList<FixedTimetableEntryDto>>;
