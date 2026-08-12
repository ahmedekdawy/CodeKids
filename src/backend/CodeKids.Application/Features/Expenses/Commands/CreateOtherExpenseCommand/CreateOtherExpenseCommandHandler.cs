using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

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
