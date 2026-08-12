using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record CreateManagedUserRequest(
    string? Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null);

public sealed record CreateManagedUserCommand(
    Guid AdminUserId,
    string? Email,
    string DisplayName,
    string Password,
    string Role,
    Guid? ParentId,
    int? Grade = null,
    string? MobilePhone = null,
    string? WorkShift = null,
    IReadOnlyList<int>? Stages = null,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    IReadOnlyList<TeacherCourseRateInput>? CourseRates = null) : ICommand<ManagedUserDto>;
