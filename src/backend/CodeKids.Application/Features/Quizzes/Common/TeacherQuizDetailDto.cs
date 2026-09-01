using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.Quizzes;

public sealed record TeacherQuizQuestionDetailDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<ChoiceOptionDto> Options,
    string CorrectOption,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    string? PromptImageUrl);

public sealed record TeacherQuizDetailDto(
    Guid Id,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string Description,
    int XpReward,
    IReadOnlyList<TeacherQuizQuestionDetailDto> Questions);
