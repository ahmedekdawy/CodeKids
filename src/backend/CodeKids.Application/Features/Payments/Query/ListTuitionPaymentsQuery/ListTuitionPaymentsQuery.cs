using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

public sealed record ListTuitionPaymentsQuery(
    Guid? ParentId = null,
    Guid? StudentId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? Year = null,
    int? Month = null) : IQuery<IReadOnlyList<TuitionPaymentDto>>;
