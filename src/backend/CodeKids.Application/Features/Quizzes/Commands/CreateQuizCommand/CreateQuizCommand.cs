using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record CreateQuizCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    bool IsPublished,
    IReadOnlyList<CreateQuizQuestionInput> Questions) : ICommand<QuizDto>;
