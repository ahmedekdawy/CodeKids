using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record CreateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    IReadOnlyList<int>? Grades,
    int? SortOrder,
    string? SchoolType = null);

public sealed record CreateCourseCommand(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    IReadOnlyList<int>? Grades,
    int? SortOrder,
    string? SchoolType = null) : ICommand<IReadOnlyList<CourseSummaryDto>>;
