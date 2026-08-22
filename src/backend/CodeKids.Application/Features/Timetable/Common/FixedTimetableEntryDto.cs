using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public sealed record FixedTimetableEntryDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    int? CourseStageId,
    IReadOnlyList<int> CombinedGrades,
    int DayOfWeek,
    int SessionNumber,
    string Period,
    string Label);
