using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record AskStudentQuestionCommand(
    Guid StudentId,
    string Question,
    Guid? CourseId,
    Guid? UnitId,
    Guid? LessonId) : ICommand<StudentAskAnswerDto>;
