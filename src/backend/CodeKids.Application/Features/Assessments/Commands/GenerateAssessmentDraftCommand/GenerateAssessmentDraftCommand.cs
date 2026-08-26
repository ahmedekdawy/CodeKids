using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Assessments;

public sealed record GenerateAssessmentDraftRequest(
    string Kind,
    Guid? CourseId,
    Guid? ClassroomId,
    IReadOnlyList<Guid>? UnitIds,
    IReadOnlyList<Guid>? LessonIds,
    int? QuestionCount,
    string? QuestionType,
    string? Language);

public sealed record GenerateAssessmentDraftCommand(
    Guid TeacherId,
    string Kind,
    Guid? CourseId,
    Guid? ClassroomId,
    IReadOnlyList<Guid>? UnitIds,
    IReadOnlyList<Guid>? LessonIds,
    int? QuestionCount,
    string? QuestionType,
    string? Language) : ICommand<GeneratedAssessmentDraftDto>;
