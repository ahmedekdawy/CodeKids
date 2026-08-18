using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record ManagedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid? ParentId,
    int? Grade,
    string? SchoolType,
    int TotalXp,
    string MobilePhone,
    string? WorkShift,
    IReadOnlyList<int> Stages,
    string? ContractType = null,
    decimal? PrimaryAmount = null,
    decimal? PrepAmount = null,
    decimal? SecondaryAmount = null,
    decimal? MonthlySalary = null,
    IReadOnlyList<TeacherCourseRateDto>? CourseRates = null);
