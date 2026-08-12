using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CreateCourseUnitRequest(
    string Title,
    string? Description = null,
    int SortOrder = 1);

public sealed record CreateCourseUnitCommand(
    Guid CourseId,
    string Title,
    string? Description = null,
    int SortOrder = 1) : ICommand<CourseUnitDto>;
