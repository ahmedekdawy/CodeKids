using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    Guid? AvatarId,
    int TotalXp,
    string MobilePhone,
    string? WorkShift,
    string? TenantId);
