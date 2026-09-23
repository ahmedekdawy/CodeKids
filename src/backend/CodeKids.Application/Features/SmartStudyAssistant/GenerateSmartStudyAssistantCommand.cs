using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.SmartStudyAssistant;

/// <summary>One uploaded file (image or PDF) attached to a generation request.</summary>
public sealed record SmartStudyAttachmentFile(
    string FileName,
    string MimeType,
    byte[] Data);

public sealed record GenerateSmartStudyAssistantRequest(
    string Action,
    Guid CourseId,
    Guid? UnitId,
    Guid? LessonId,
    string? Language);

public sealed record GenerateSmartStudyAssistantCommand(
    Guid TeacherId,
    string Action,
    Guid CourseId,
    Guid? UnitId,
    Guid? LessonId,
    string? Language,
    IReadOnlyList<SmartStudyAttachmentFile> Attachments) : ICommand<SmartStudyAssistantResultDto>;

public sealed record SmartStudyAssistantQuestionDto(
    string Prompt,
    string QuestionType,
    IReadOnlyList<string> Options,
    string CorrectOption,
    string CorrectAnswer,
    int Points,
    int SortOrder);

public sealed record SmartStudyAssistantUnitDto(
    string Title,
    int SortOrder,
    IReadOnlyList<string> Lessons);

public sealed record SmartStudyAssistantResultDto(
    string Action,
    string Title,
    string Markdown,
    IReadOnlyList<SmartStudyAssistantQuestionDto> Questions,
    IReadOnlyList<SmartStudyAssistantUnitDto> Units);
