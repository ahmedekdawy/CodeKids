using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record DeleteStudentAskedQuestionCommand(
    Guid ActorId,
    string? ActorRole,
    Guid QuestionId) : ICommand<bool>;
