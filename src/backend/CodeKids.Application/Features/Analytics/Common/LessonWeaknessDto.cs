using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record LessonWeaknessDto(
    Guid LessonId,
    string LessonTitle,
    int WrongAnswers,
    int TotalAnswers,
    int AccuracyPercent);
