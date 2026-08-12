using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record TeacherStudentDetailDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    string? MobilePhone,
    string? ParentName,
    string? ParentMobilePhone,
    int TotalXp,
    StudentLevelDto Level,
    int CompletedSteps,
    int QuizAttempts,
    int ExamAttempts,
    int AssignmentSubmissions,
    IReadOnlyList<LessonMasteryDto> LessonMastery,
    IReadOnlyList<LessonWeaknessDto> WeakLessons,
    IReadOnlyList<WatchSummaryDto> RecentWatch);
