using CodeKids.Application.Features.Analytics;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed record TeacherDashboardDto(
    Guid TeacherId,
    string TeacherName,
    int StudentCount,
    int TotalCompletedSteps,
    int AverageXp,
    int BehindCount,
    IReadOnlyList<string> TopWeakLessons,
    IReadOnlyList<TeacherStudentDto> Students);
