using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    string AnswerText,
    string? CorrectAnswer,
    bool? IsCorrect,
    int? PointsAwarded,
    int Points,
    string? PromptImageUrl,
    string? AnswerImageUrl,
    string QuestionType,
    IReadOnlyList<ChoiceOptionDto> Options);
