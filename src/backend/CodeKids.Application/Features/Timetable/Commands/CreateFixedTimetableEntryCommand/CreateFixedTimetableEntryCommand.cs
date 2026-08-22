using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public sealed record CreateFixedTimetableEntryRequest(
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    string Period,
    IReadOnlyList<int>? CombinedGrades = null);

public sealed record CreateFixedTimetableEntryCommand(
    Guid TeacherId,
    Guid CourseId,
    int DayOfWeek,
    int SessionNumber,
    TimetablePeriod Period,
    IReadOnlyList<int>? CombinedGrades = null) : ICommand<FixedTimetableEntryDto>;
