using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

public sealed record OtherExpenseDto(
    Guid Id,
    string Name,
    decimal Amount,
    DateOnly ExpenseDate,
    string Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateOtherExpenseRequest(
    string Name,
    decimal Amount,
    DateOnly ExpenseDate,
    string? Notes = null);

public sealed record CreateOtherExpenseCommand(
    string Name,
    decimal Amount,
    DateOnly ExpenseDate,
    string? Notes = null) : ICommand<OtherExpenseDto>;

public sealed record ListOtherExpensesQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Name = null) : IQuery<IReadOnlyList<OtherExpenseDto>>;

public sealed record DeleteOtherExpenseCommand(Guid ExpenseId) : ICommand<bool>;

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

public sealed class CreateOtherExpenseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateOtherExpenseCommand, OtherExpenseDto>
{
    public async Task<OtherExpenseDto> Handle(
        CreateOtherExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var name = (command.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Expense name is required.");
        }

        if (name.Length > 200)
        {
            throw new InvalidOperationException("Expense name is too long.");
        }

        if (command.Amount <= 0)
        {
            throw new InvalidOperationException("Expense amount must be greater than zero.");
        }

        if (command.ExpenseDate == default)
        {
            throw new InvalidOperationException("Expense date is required.");
        }

        var notes = (command.Notes ?? string.Empty).Trim();
        if (notes.Length > 500)
        {
            notes = notes[..500];
        }

        var row = new OtherExpense
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero),
            ExpenseDate = command.ExpenseDate,
            Notes = notes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.OtherExpenses.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OtherExpenseDto(
            row.Id,
            row.Name,
            row.Amount,
            row.ExpenseDate,
            row.Notes,
            row.CreatedAtUtc);
    }
}

public sealed class DeleteOtherExpenseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteOtherExpenseCommand, bool>
{
    public async Task<bool> Handle(DeleteOtherExpenseCommand command, CancellationToken cancellationToken)
    {
        var row = await dbContext.OtherExpenses
            .FirstOrDefaultAsync(x => x.Id == command.ExpenseId, cancellationToken)
            ?? throw new InvalidOperationException("Other expense not found.");

        dbContext.OtherExpenses.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
