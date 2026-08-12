using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

public sealed class CreateTuitionPaymentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateTuitionPaymentCommand, TuitionPaymentDto>
{
    public async Task<TuitionPaymentDto> Handle(
        CreateTuitionPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var (parentId, studentId) = await TuitionPaymentValidators.ValidateAsync(
            dbContext,
            command.ParentId,
            command.StudentId,
            command.Year,
            command.Month,
            command.Amount,
            command.PaymentDate,
            cancellationToken);

        var row = new TuitionPayment
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            StudentId = studentId,
            Year = command.Year,
            Month = command.Month,
            Amount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero),
            PaymentDate = command.PaymentDate,
            Notes = (command.Notes ?? string.Empty).Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.TuitionPayments.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await TuitionPaymentValidators.LoadDtoAsync(dbContext, row.Id, cancellationToken);
    }
}
