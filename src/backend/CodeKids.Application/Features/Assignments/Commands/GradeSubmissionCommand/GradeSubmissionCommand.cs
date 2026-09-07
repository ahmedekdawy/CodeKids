using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record GradeSubmissionRequest(
    Guid SubmissionId,
    string? TeacherFeedback,
    Guid? FeedbackImageMediaAssetId,
    IReadOnlyList<GradeAnswerInput>? Answers);

public sealed record GradeSubmissionCommand(
    Guid TeacherUserId,
    Guid SubmissionId,
    string? TeacherFeedback,
    Guid? FeedbackImageMediaAssetId,
    IReadOnlyList<GradeAnswerInput>? Answers) : ICommand<AssignmentSubmissionDto>;
