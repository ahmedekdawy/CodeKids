using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId);

/// <summary>Email holds email or mobile login identifier.</summary>

public sealed record RegisterCommand(
    string Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId) : ICommand<AuthResponse>;
