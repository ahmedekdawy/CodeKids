using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentSubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid StudentId,
    string StudentName,
    string Status,
    int? Score,
    int? MaxScore,
    string? TeacherFeedback,
    string? FeedbackImageUrl,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? GradedAtUtc,
    Guid? SolutionVideoMediaAssetId,
    IReadOnlyList<AssignmentAnswerReviewDto> Answers);
