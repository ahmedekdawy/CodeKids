using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CourseDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string? Term,
    int? Grade,
    int SortOrder,
    IReadOnlyList<CourseLessonDto> Lessons,
    IReadOnlyList<CourseQuizDto> Quizzes);
