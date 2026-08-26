namespace CodeKids.Application.Features.StudentAsk;

public sealed record SetStudentAskEnabledRequest(string Scope, Guid Id, bool Enabled);

public sealed record StudentAskSettingsDto(string Scope, Guid Id, bool Enabled);

public sealed record AskStudentQuestionRequest(
    string Question,
    Guid? CourseId = null,
    Guid? UnitId = null,
    Guid? LessonId = null);

public sealed record StudentAskAnswerDto(bool InScope, string Answer);
