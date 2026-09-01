using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record DeleteQuizCommand(Guid TeacherUserId, Guid QuizId) : ICommand<bool>;
