using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Payments;

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
