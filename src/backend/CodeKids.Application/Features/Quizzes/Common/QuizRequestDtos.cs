namespace CodeKids.Application.Features.Quizzes;

public sealed record CreateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions);

public sealed record UpdateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions);

public sealed record CreateQuizQuestionInput(
    string Prompt,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string CorrectOption,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    Guid? Id = null);
