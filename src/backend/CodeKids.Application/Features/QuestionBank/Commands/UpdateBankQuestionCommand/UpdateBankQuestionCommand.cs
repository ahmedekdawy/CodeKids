using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record UpdateBankQuestionRequest(
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId = null,
    IReadOnlyList<MapMarkerInput>? MapMarkers = null);

public sealed record UpdateBankQuestionCommand(
    Guid TeacherUserId,
    Guid QuestionId,
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId = null,
    IReadOnlyList<MapMarkerInput>? MapMarkers = null) : ICommand<BankQuestionDto>;
