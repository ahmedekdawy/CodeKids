using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record DeleteTeacherSessionAttendanceCommand(
    Guid AttendanceId,
    Guid? ActingTeacherId = null) : ICommand<bool>;
