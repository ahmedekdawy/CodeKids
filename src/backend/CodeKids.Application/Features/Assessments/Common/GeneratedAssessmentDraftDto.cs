namespace CodeKids.Application.Features.Assessments;

public sealed record GeneratedAssessmentDraftDto(
    string Kind,
    string Title,
    string Description,
    IReadOnlyList<GeneratedAssessmentQuestionDto> Questions,
    IReadOnlyList<Guid> QuestionIds);

public sealed record GeneratedAssessmentQuestionDto(
    string Prompt,
    string QuestionType,
    IReadOnlyList<string> Options,
    string CorrectOption,
    string CorrectAnswer,
    int Points,
    int SortOrder);
