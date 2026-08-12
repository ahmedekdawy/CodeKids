using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record ExamAttemptDto(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentId,
    string StudentName,
    string Status,
    int? Score,
    int? MaxScore,
    string? TeacherFeedback,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? GradedAtUtc,
    int? DurationSeconds,
    IReadOnlyList<ExamAnswerReviewDto> Answers);
