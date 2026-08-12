using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Expenses;

public sealed record DeleteOtherExpenseCommand(Guid ExpenseId) : ICommand<bool>;
