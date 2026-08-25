using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record GetCourseByIdQuery(
    Guid CourseId,
    Guid? UserId = null,
    string? Role = null) : IQuery<CourseDto?>;
