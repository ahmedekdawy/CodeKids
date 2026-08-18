using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record GetQuizAttemptsQuery(Guid TeacherUserId, Guid QuizId)
    : IQuery<IReadOnlyList<QuizAttemptReviewDto>>;
