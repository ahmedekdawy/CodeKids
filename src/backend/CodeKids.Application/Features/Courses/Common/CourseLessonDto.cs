using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CourseLessonDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int Difficulty,
    int XpReward,
    int SortOrder,
    int StepCount);
