using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record ListTeacherSessionAttendanceQuery(
    Guid? TeacherId = null,
    int? CourseGrade = null,
    DateOnly? SessionDate = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IQuery<IReadOnlyList<TeacherSessionAttendanceDto>>;
