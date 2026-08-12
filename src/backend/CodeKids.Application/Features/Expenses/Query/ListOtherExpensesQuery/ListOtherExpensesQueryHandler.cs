using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

public sealed class ListOtherExpensesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListOtherExpensesQuery, IReadOnlyList<OtherExpenseDto>>
{
    public async Task<IReadOnlyList<OtherExpenseDto>> Handle(
        ListOtherExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.OtherExpenses.AsNoTracking().AsQueryable();

        if (query.FromDate.HasValue)
        {
            rows = rows.Where(x => x.ExpenseDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            rows = rows.Where(x => x.ExpenseDate <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim().ToLowerInvariant();
            rows = rows.Where(x => x.Name.ToLower().Contains(term));
        }

        return await rows
            .OrderByDescending(x => x.ExpenseDate)
            .ThenBy(x => x.Name)
            .Select(x => new OtherExpenseDto(
                x.Id,
                x.Name,
                x.Amount,
                x.ExpenseDate,
                x.Notes,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
