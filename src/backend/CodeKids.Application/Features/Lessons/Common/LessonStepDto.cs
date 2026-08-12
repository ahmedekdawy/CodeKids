using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record LessonStepDto(
    Guid Id,
    int StepNumber,
    string Title,
    string Prompt);
