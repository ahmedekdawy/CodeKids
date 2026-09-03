namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentQuestionInput(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    Guid? Id = null,
    string? PassageText = null,
    IReadOnlyList<string>? Options = null,
    IReadOnlyList<AssignmentQuestionInput>? Children = null);
