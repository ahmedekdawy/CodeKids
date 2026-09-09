namespace CodeKids.Application.Features.Quizzes;

public sealed record QuizAnswerDto(
    Guid QuestionId,
    string SelectedOption,
    Guid? AnswerImageMediaAssetId = null);
