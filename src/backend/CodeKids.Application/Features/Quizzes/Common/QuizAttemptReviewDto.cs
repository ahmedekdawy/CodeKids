namespace CodeKids.Application.Features.Quizzes;

public sealed record QuizAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    int SortOrder,
    string SelectedOption,
    string SelectedText,
    string CorrectOption,
    string CorrectText,
    bool IsCorrect,
    string? PromptImageUrl);

public sealed record QuizAttemptReviewDto(
    Guid Id,
    Guid QuizId,
    Guid StudentId,
    string StudentName,
    int Score,
    int TotalQuestions,
    int EarnedXp,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<QuizAnswerReviewDto> Answers);
