using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assignments;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Exams;

public sealed record GradeExamAttemptRequest(
    Guid AttemptId,
    string? TeacherFeedback,
    Guid? FeedbackImageMediaAssetId,
    IReadOnlyList<GradeAnswerInput>? Answers);

public sealed record GradeExamAttemptCommand(
    Guid TeacherUserId,
    Guid AttemptId,
    string? TeacherFeedback,
    Guid? FeedbackImageMediaAssetId,
    IReadOnlyList<GradeAnswerInput>? Answers) : ICommand<ExamAttemptDto>;
