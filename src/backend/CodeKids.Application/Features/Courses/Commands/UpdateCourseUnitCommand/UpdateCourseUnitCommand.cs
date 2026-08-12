using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record UpdateCourseUnitRequest(
    string Title,
    string? Description = null,
    int SortOrder = 1);

public sealed record UpdateCourseUnitCommand(
    Guid UnitId,
    string Title,
    string? Description = null,
    int SortOrder = 1) : ICommand<CourseUnitDto>;
