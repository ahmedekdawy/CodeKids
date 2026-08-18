using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Quizzes;

public sealed record GetTeacherQuizzesQuery(
    Guid TeacherUserId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int? Grade,
    Guid? CourseId) : IQuery<IReadOnlyList<TeacherQuizListDto>>;
