using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed record TeacherStudentDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    int TotalXp,
    int LevelNumber,
    string LevelName,
    int LevelProgressPercent,
    int CompletedSteps,
    int QuizAttempts,
    int WeakLessonCount,
    string? ParentName,
    string? Signal);
