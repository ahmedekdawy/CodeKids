using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Attendance;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Reports;

public sealed record AccountReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalPayrollSalaries,
    decimal TotalManualSalaries,
    decimal TotalSalaries,
    decimal TotalSubscriptions,
    decimal TotalOtherExpenses,
    decimal NetAmount);
