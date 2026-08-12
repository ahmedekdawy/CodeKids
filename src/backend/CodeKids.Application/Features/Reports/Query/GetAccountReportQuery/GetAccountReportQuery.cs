using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Attendance;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Reports;

public sealed record GetAccountReportQuery(
    DateOnly FromDate,
    DateOnly ToDate) : IQuery<AccountReportDto>;
