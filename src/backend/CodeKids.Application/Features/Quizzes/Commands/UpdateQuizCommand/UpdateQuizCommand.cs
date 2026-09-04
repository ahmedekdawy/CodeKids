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
    bool IsPublished,
    IReadOnlyList<CreateQuizQuestionInput> Questions,
    int? DurationMinutes = null) : ICommand<QuizDto>;
