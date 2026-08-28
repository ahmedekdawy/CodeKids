using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class DeleteStudentAskedQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteStudentAskedQuestionCommand, bool>
{
    public async Task<bool> Handle(
        DeleteStudentAskedQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.StudentAskedQuestions
            .FirstOrDefaultAsync(x => x.Id == command.QuestionId, cancellationToken)
            ?? throw new InvalidOperationException("Asked question not found.");

        if (string.Equals(command.ActorRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
        {
            if (row.StudentId != command.ActorId)
            {
                throw new InvalidOperationException("You can only delete your own questions.");
            }
        }
        else
        {
            await CourseTreeAccess.EnsureCanManageCourseAsync(
                dbContext,
                command.ActorId,
                command.ActorRole,
                row.CourseId,
                cancellationToken);
        }

        dbContext.StudentAskedQuestions.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
