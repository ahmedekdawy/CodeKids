namespace CodeKids.Application.Features.Quizzes;

public sealed record TeacherQuizListDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    int? CourseGrade,
    Guid? ClassroomId,
    string? ClassroomName,
    string Title,
    string Description,
    int XpReward,
    int QuestionCount,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc);
