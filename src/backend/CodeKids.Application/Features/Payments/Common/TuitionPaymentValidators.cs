using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

internal static class TuitionPaymentValidators
{
    public static async Task<(Guid? ParentId, Guid? StudentId)> ValidateAsync(
        IAppDbContext dbContext,
        Guid? parentId,
        Guid? studentId,
        int year,
        int month,
        decimal amount,
        DateOnly paymentDate,
        CancellationToken cancellationToken)
    {
        var hasParent = parentId.HasValue && parentId.Value != Guid.Empty;
        var hasStudent = studentId.HasValue && studentId.Value != Guid.Empty;

        if (hasParent == hasStudent)
        {
            throw new InvalidOperationException("Select either a parent or a student without a parent.");
        }

        if (year is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Payment year is invalid.");
        }

        if (month is < 1 or > 12)
        {
            throw new InvalidOperationException("Payment month must be between 1 and 12.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        if (paymentDate == default)
        {
            throw new InvalidOperationException("Payment date is required.");
        }

        if (hasParent)
        {
            var parent = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentId, cancellationToken)
                ?? throw new InvalidOperationException("Parent not found.");

            if (parent.Role != UserRole.Parent)
            {
                throw new InvalidOperationException("Selected user must be a parent.");
            }

            return (parent.Id, null);
        }

        var student = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        if (student.Role != UserRole.Student)
        {
            throw new InvalidOperationException("Selected user must be a student.");
        }

        if (student.ParentId.HasValue)
        {
            throw new InvalidOperationException("This student has a parent; record the payment under the parent.");
        }

        return (null, student.Id);
    }

    public static async Task<TuitionPaymentDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TuitionPayments
            .AsNoTracking()
            .Include(x => x.Parent)
            .Include(x => x.Student)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return ToDto(row);
    }

    public static TuitionPaymentDto ToDto(TuitionPayment row)
    {
        var parentName = row.Parent?.DisplayName;
        var studentName = row.Student?.DisplayName;
        var payerLabel = !string.IsNullOrWhiteSpace(parentName)
            ? parentName
            : (studentName ?? string.Empty);

        return new TuitionPaymentDto(
            row.Id,
            row.ParentId,
            parentName,
            row.StudentId,
            studentName,
            row.Year,
            row.Month,
            row.Amount,
            row.PaymentDate,
            row.Notes,
            payerLabel);
    }
}
