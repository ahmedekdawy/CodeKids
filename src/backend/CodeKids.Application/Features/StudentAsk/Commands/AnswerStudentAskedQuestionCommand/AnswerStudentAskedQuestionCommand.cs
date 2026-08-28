using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record AnswerStudentAskedQuestionCommand(
    Guid TeacherId,
    string? TeacherRole,
    Guid QuestionId,
    string Answer) : ICommand<StudentAskedQuestionDto>;
