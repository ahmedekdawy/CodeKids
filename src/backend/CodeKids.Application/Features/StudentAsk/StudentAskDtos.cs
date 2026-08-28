namespace CodeKids.Application.Features.StudentAsk;

public sealed record SetStudentAskEnabledRequest(string Scope, Guid Id, bool Enabled);

public sealed record StudentAskSettingsDto(string Scope, Guid Id, bool Enabled);

public sealed record AskStudentQuestionRequest(
    string Question,
    Guid? CourseId = null,
    Guid? UnitId = null,
    Guid? LessonId = null);

public sealed record StudentAskAnswerDto(bool InScope, string Answer);

public sealed record AnswerStudentAskedQuestionRequest(string Answer);

public sealed record UpdateStudentAskedQuestionRequest(string Question);

public sealed record StudentAskedQuestionDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid? UnitId,
    string UnitTitle,
    Guid? LessonId,
    string LessonTitle,
    string Question,
    string AiAnswer,
    bool AiInScope,
    string TeacherAnswer,
    string TeacherName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? TeacherAnsweredAtUtc,
    bool IsMine);
