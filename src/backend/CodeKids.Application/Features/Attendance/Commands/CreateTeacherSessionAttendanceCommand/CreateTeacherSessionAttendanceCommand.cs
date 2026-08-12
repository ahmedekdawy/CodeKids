using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record CreateTeacherSessionAttendanceRequest(
    Guid TeacherId,
    Guid CourseId,
    DateOnly SessionDate);

public sealed record CreateTeacherSessionAttendanceCommand(
    Guid TeacherId,
    Guid CourseId,
    DateOnly SessionDate) : ICommand<TeacherSessionAttendanceDto>;
