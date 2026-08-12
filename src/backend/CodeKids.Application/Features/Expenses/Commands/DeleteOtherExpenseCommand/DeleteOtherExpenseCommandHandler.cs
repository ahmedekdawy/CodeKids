using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

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
