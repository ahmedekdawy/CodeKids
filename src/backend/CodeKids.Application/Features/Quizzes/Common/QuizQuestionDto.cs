using CodeKids.Application.Features.QuestionBank;

namespace CodeKids.Application.Features.Quizzes;

public sealed record QuizQuestionDto(
    Guid Id,
    string Prompt,
    string QuestionType,
    string PassageText,
    string OptionA,
    string OptionB,
    string OptionC,
    IReadOnlyList<ChoiceOptionDto> Options,
    int SortOrder,
    string? PromptImageUrl,
    IReadOnlyList<QuizQuestionDto> Children);
