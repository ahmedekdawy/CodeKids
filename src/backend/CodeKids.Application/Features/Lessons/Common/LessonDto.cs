using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record LessonDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int Difficulty,
    int XpReward,
    IReadOnlyList<LessonStepDto> Steps,
    IReadOnlyList<LessonVideoSummaryDto> Videos);
