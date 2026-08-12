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
