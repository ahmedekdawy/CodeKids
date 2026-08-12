using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Courses;

public sealed record DeleteCourseLessonCommand(Guid LessonId) : ICommand<bool>;
