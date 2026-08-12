using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record TeacherSessionAttendanceDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    DateOnly SessionDate,
    string Label);
