namespace CodeKids.Application.Features.Quizzes;

public sealed record CreateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<CreateQuizQuestionInput> Questions,
    int? DurationMinutes = null);

public sealed record UpdateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<CreateQuizQuestionInput> Questions,
    int? DurationMinutes = null);

public sealed record CreateQuizQuestionInput(
    string Prompt,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string? CorrectOption,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    Guid? Id = null,
    string? QuestionType = null,
    string? PassageText = null,
    string? CorrectAnswer = null,
    int Points = 1,
    IReadOnlyList<CreateQuizQuestionInput>? Children = null);
