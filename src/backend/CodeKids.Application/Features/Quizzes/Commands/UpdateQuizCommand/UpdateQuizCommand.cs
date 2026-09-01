using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record UpdateQuizCommand(
    Guid TeacherUserId,
    Guid QuizId,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions) : ICommand<QuizDto>;
