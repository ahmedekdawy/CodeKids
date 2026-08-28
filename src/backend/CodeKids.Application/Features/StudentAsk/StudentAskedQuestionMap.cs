using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.StudentAsk;

internal static class StudentAskedQuestionMap
{
    public static StudentAskedQuestionDto ToDto(StudentAskedQuestion row, bool isMine) =>
        new(
            row.Id,
            row.StudentId,
            row.StudentName,
            row.CourseId,
            row.CourseTitle,
            row.UnitId,
            row.UnitTitle,
            row.LessonId,
            row.LessonTitle,
            row.Question,
            row.AiAnswer,
            row.AiInScope,
            row.TeacherAnswer,
            row.Teacher?.DisplayName ?? string.Empty,
            row.CreatedAtUtc,
            row.TeacherAnsweredAtUtc,
            isMine);
}
