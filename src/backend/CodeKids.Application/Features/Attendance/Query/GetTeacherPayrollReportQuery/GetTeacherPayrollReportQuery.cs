using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed record GetTeacherPayrollReportQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? TeacherId = null,
    int? Stage = null,
    int? Grade = null) : IQuery<TeacherPayrollReportDto>;
