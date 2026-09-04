using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record CreateExamRequest(
    Guid ClassroomId,
    Guid? CourseId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<Guid> QuestionIds,
    int? DurationMinutes = null);

public sealed record CreateExamCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    Guid? CourseId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<Guid> QuestionIds,
    int? DurationMinutes = null) : ICommand<ExamDto>;
