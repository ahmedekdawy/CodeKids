using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class AnswerStudentAskedQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AnswerStudentAskedQuestionCommand, StudentAskedQuestionDto>
{
    private const int MaxAnswerLength = 4000;

    public async Task<StudentAskedQuestionDto> Handle(
        AnswerStudentAskedQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var answer = (command.Answer ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Answer is required.");
        }

        if (answer.Length > MaxAnswerLength)
        {
            throw new InvalidOperationException("Answer is too long.");
        }

        var row = await dbContext.StudentAskedQuestions
            .FirstOrDefaultAsync(x => x.Id == command.QuestionId, cancellationToken)
            ?? throw new InvalidOperationException("Asked question not found.");

        await CourseTreeAccess.EnsureCanManageCourseAsync(
            dbContext,
            command.TeacherId,
            command.TeacherRole,
            row.CourseId,
            cancellationToken);

        row.TeacherAnswer = answer;
        row.TeacherId = command.TeacherId;
        row.TeacherAnsweredAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var teacherName = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == command.TeacherId)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new StudentAskedQuestionDto(
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
            teacherName,
            row.CreatedAtUtc,
            row.TeacherAnsweredAtUtc,
            false);
    }
}
