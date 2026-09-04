using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record GetLessonsQuery(Guid? CourseId = null, string? ViewerRole = null) : IQuery<IReadOnlyList<LessonDto>>;
