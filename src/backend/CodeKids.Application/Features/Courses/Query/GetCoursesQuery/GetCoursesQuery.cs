using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record GetCoursesQuery(
    Guid? UserId = null,
    string? Role = null,
    bool IncludeContent = true) : IQuery<IReadOnlyList<CourseDto>>;
