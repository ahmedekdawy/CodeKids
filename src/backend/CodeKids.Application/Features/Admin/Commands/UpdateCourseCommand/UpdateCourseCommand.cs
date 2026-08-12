using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record UpdateCourseRequest(
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    int? Grade,
    int? SortOrder);

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int? AgeMin,
    int? AgeMax,
    string? Term,
    int? Grade,
    int? SortOrder) : ICommand<CourseSummaryDto>;
