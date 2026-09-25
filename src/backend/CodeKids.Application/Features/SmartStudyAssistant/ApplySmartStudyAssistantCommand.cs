using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.SmartStudyAssistant;

public sealed record ApplySmartStudyAssistantQuestionInput(
    string Prompt,
    string QuestionType,
    IReadOnlyList<string> Options,
    string CorrectOption,
    string CorrectAnswer,
    int Points,
    int SortOrder = 1);

public sealed record ApplySmartStudyAssistantUnitInput(
    string Title,
    int SortOrder,
    IReadOnlyList<string> Lessons);

public sealed record ApplySmartStudyAssistantRequest(
    Guid CourseId,
    string Target,
    string Title,
    string? Description,
    Guid? UnitId,
    Guid? LessonId,
    string? Mode,
    IReadOnlyList<ApplySmartStudyAssistantQuestionInput>? Questions,
    IReadOnlyList<ApplySmartStudyAssistantUnitInput>? Units);

public sealed record ApplySmartStudyAssistantCommand(
    Guid TeacherId,
    Guid CourseId,
    string Target,
    string Title,
    string? Description,
    Guid? UnitId,
    Guid? LessonId,
    string? Mode,
    IReadOnlyList<ApplySmartStudyAssistantQuestionInput>? Questions,
    IReadOnlyList<ApplySmartStudyAssistantUnitInput>? Units) : ICommand<ApplySmartStudyAssistantResultDto>;

public sealed record ApplySmartStudyAssistantResultDto(
    string Target,
    Guid? ResourceId,
    string? ResourceUrl,
    int QuestionsCreated,
    int UnitsCreated,
    int LessonsCreated,
    IReadOnlyList<Guid> QuestionIds,
    string Message);
