using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

public sealed record CreateOtherExpenseRequest(
    string Name,
    decimal Amount,
    DateOnly ExpenseDate,
    string? Notes = null);

public sealed record CreateOtherExpenseCommand(
    string Name,
    decimal Amount,
    DateOnly ExpenseDate,
    string? Notes = null) : ICommand<OtherExpenseDto>;
