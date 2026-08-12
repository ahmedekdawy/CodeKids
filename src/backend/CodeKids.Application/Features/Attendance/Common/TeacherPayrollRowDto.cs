using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record TeacherPayrollRowDto(
    Guid TeacherId,
    string TeacherName,
    int PrimarySessions,
    int PrepSessions,
    int SecondarySessions,
    decimal TotalAmount);
