using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentQuestionDto(
    Guid Id,
    string Prompt,
    string QuestionType,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<ChoiceOptionDto> Options,
    int Points,
    int SortOrder,
    string? CorrectAnswer,
    string? PromptImageUrl,
    Guid? PromptImageMediaAssetId,
    IReadOnlyList<AssignmentQuestionDto> Children);
