using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record GetTeacherQuizByIdQuery(Guid TeacherUserId, Guid QuizId)
    : IQuery<TeacherQuizDetailDto?>;
