using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.Quizzes;

public sealed record TeacherQuizQuestionDetailDto(
    Guid Id,
    string Prompt,
    string QuestionType,
    string PassageText,
    IReadOnlyList<ChoiceOptionDto> Options,
    string CorrectOption,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    string? PromptImageUrl,
    IReadOnlyList<TeacherQuizQuestionDetailDto> Children);

public sealed record TeacherQuizDetailDto(
    Guid Id,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string Description,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<TeacherQuizQuestionDetailDto> Questions);
