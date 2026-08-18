using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Attendance;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Reports;

public sealed class GetAccountReportQueryHandler(
    IAppDbContext dbContext,
    IQueryHandler<GetTeacherPayrollReportQuery, TeacherPayrollReportDto> payrollHandler)
    : IQueryHandler<GetAccountReportQuery, AccountReportDto>
{
    public async Task<AccountReportDto> Handle(
        GetAccountReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ToDate < query.FromDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var payroll = await payrollHandler.Handle(
            new GetTeacherPayrollReportQuery(query.FromDate, query.ToDate),
            cancellationToken);

        var totalSubscriptions = await dbContext.TuitionPayments
            .AsNoTracking()
            .Where(x => x.PaymentDate >= query.FromDate && x.PaymentDate <= query.ToDate)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var totalOtherExpenses = await dbContext.OtherExpenses
            .AsNoTracking()
            .Where(x => x.ExpenseDate >= query.FromDate && x.ExpenseDate <= query.ToDate)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var totalPayrollSalaries = Math.Round(
            payroll.Rows.Sum(x => x.SessionAmount + x.MonthlySalary),
            2,
            MidpointRounding.AwayFromZero);

        var totalManualSalaries = await dbContext.TeacherPayrollAdjustments
            .AsNoTracking()
            .Where(x => x.AdjustmentDate >= query.FromDate && x.AdjustmentDate <= query.ToDate)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        totalManualSalaries = Math.Round(totalManualSalaries, 2, MidpointRounding.AwayFromZero);

        var totalSalaries = Math.Round(
            totalPayrollSalaries + totalManualSalaries,
            2,
            MidpointRounding.AwayFromZero);

        var netAmount = Math.Round(
            totalSubscriptions - totalSalaries - totalOtherExpenses,
            2,
            MidpointRounding.AwayFromZero);

        return new AccountReportDto(
            query.FromDate,
            query.ToDate,
            totalPayrollSalaries,
            totalManualSalaries,
            totalSalaries,
            Math.Round(totalSubscriptions, 2, MidpointRounding.AwayFromZero),
            Math.Round(totalOtherExpenses, 2, MidpointRounding.AwayFromZero),
            netAmount);
    }
}
