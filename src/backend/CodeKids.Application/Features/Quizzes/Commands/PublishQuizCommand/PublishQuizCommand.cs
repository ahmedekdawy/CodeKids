using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record PublishQuizCommand(Guid TeacherUserId, Guid QuizId) : ICommand<QuizDto>;
