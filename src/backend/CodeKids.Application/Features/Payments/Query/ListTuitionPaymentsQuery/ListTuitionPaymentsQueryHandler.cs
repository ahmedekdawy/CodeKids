using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

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
