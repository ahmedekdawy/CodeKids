using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

public sealed class DeleteTuitionPaymentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteTuitionPaymentCommand, bool>
{
    public async Task<bool> Handle(DeleteTuitionPaymentCommand command, CancellationToken cancellationToken)
    {
        var row = await dbContext.TuitionPayments
            .FirstOrDefaultAsync(x => x.Id == command.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException("Tuition payment not found.");

        dbContext.TuitionPayments.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
