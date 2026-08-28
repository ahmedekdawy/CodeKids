using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record ListStudentAskedQuestionsQuery(
    Guid ViewerId,
    string? ViewerRole,
    Guid? CourseId,
    Guid? UnitId,
    Guid? LessonId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? QuestionText) : IQuery<IReadOnlyList<StudentAskedQuestionDto>>;
