using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

public sealed record TuitionPaymentDto(
    Guid Id,
    Guid? ParentId,
    string? ParentName,
    Guid? StudentId,
    string? StudentName,
    int Year,
    int Month,
    decimal Amount,
    DateOnly PaymentDate,
    string Notes,
    string PayerLabel);

public sealed record CreateTuitionPaymentRequest(
    Guid? ParentId,
    Guid? StudentId,
    int Year,
    int Month,
    decimal Amount,
    DateOnly PaymentDate,
    string? Notes = null);

public sealed record CreateTuitionPaymentCommand(
    Guid? ParentId,
    Guid? StudentId,
    int Year,
    int Month,
    decimal Amount,
    DateOnly PaymentDate,
    string? Notes = null) : ICommand<TuitionPaymentDto>;

public sealed record ListTuitionPaymentsQuery(
    Guid? ParentId = null,
    Guid? StudentId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? Year = null,
    int? Month = null) : IQuery<IReadOnlyList<TuitionPaymentDto>>;

public sealed record DeleteTuitionPaymentCommand(Guid PaymentId) : ICommand<bool>;

public sealed class ListTuitionPaymentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTuitionPaymentsQuery, IReadOnlyList<TuitionPaymentDto>>
{
    public async Task<IReadOnlyList<TuitionPaymentDto>> Handle(
        ListTuitionPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.TuitionPayments
            .AsNoTracking()
            .Include(x => x.Parent)
            .Include(x => x.Student)
            .AsQueryable();

        if (query.ParentId.HasValue)
        {
            rows = rows.Where(x => x.ParentId == query.ParentId.Value);
        }

        if (query.StudentId.HasValue)
        {
            rows = rows.Where(x => x.StudentId == query.StudentId.Value);
        }

        if (query.FromDate.HasValue)
        {
            rows = rows.Where(x => x.PaymentDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            rows = rows.Where(x => x.PaymentDate <= query.ToDate.Value);
        }

        if (query.Year.HasValue)
        {
            rows = rows.Where(x => x.Year == query.Year.Value);
        }

        if (query.Month.HasValue)
        {
            rows = rows.Where(x => x.Month == query.Month.Value);
        }

        return (await rows
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ThenBy(x => x.Parent!.DisplayName)
            .ThenBy(x => x.Student!.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(TuitionPaymentValidators.ToDto)
            .ToList();
    }
}

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
