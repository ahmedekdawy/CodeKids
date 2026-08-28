using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record UpdateStudentAskedQuestionCommand(
    Guid TeacherId,
    string? TeacherRole,
    Guid QuestionId,
    string Question) : ICommand<StudentAskedQuestionDto>;
