using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record GetCoursesQuery(Guid? UserId = null, string? Role = null) : IQuery<IReadOnlyList<CourseDto>>;
