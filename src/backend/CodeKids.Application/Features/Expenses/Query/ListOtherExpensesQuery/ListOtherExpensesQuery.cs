using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

public sealed record ListOtherExpensesQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Name = null) : IQuery<IReadOnlyList<OtherExpenseDto>>;
