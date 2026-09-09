using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record CreateBankQuestionRequest(
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
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
    Guid? PromptImageMediaAssetId,
    IReadOnlyList<BankChildQuestionInput>? Children,
    IReadOnlyList<MapMarkerInput>? MapMarkers = null);

public sealed record CreateBankQuestionCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
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
    Guid? PromptImageMediaAssetId,
    IReadOnlyList<BankChildQuestionInput>? Children,
    IReadOnlyList<MapMarkerInput>? MapMarkers = null) : ICommand<BankQuestionDto>;
