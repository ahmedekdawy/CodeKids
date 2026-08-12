using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record TeacherPayrollReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<TeacherPayrollRowDto> Rows,
    decimal GrandTotal);
