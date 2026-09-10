using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record BankQuestionDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid? LessonId,
    string? LessonTitle,
    Guid CreatedByUserId,
    Guid? ParentQuestionId,
    string QuestionType,
    string Prompt,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<ChoiceOptionDto> Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    string? PromptImageUrl,
    IReadOnlyList<BankQuestionDto> Children,
    Guid? PromptImageMediaAssetId = null,
    IReadOnlyList<MapMarkerDto>? MapMarkers = null);
