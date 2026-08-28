using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class UpdateStudentAskedQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateStudentAskedQuestionCommand, StudentAskedQuestionDto>
{
    private const int MaxQuestionLength = 800;

    public async Task<StudentAskedQuestionDto> Handle(
        UpdateStudentAskedQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var question = (command.Question ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException("Question is required.");
        }

        if (question.Length > MaxQuestionLength)
        {
            throw new InvalidOperationException("Question is too long.");
        }

        var row = await dbContext.StudentAskedQuestions
            .Include(x => x.Teacher)
            .FirstOrDefaultAsync(x => x.Id == command.QuestionId, cancellationToken)
            ?? throw new InvalidOperationException("Asked question not found.");

        await CourseTreeAccess.EnsureCanManageCourseAsync(
            dbContext,
            command.TeacherId,
            command.TeacherRole,
            row.CourseId,
            cancellationToken);

        row.Question = question;
        await dbContext.SaveChangesAsync(cancellationToken);

        return StudentAskedQuestionMap.ToDto(row, false);
    }
}
