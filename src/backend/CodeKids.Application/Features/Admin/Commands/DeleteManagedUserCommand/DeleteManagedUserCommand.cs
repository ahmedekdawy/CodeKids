using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

/// <param name="DeleteRelatedData">
/// True deletes everything that belongs to the user. False keeps it by moving it to the admin's account first.
/// Ignored for students, whose data is always deleted with them.
/// </param>
public sealed record DeleteManagedUserCommand(Guid AdminUserId, Guid UserId, bool DeleteRelatedData = false) : ICommand<bool>;
