using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class DeleteAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteAssignmentCommand, bool>
{
    public async Task<bool> Handle(DeleteAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .Include(x => x.Submissions)
                .ThenInclude(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        await AssignmentAuthorization.EnsureCanManageClassroomAsync(
            dbContext, command.TeacherUserId, assignment.ClassroomId, "delete", cancellationToken);

        dbContext.AssignmentAnswers.RemoveRange(assignment.Submissions.SelectMany(s => s.Answers));
        dbContext.AssignmentSubmissions.RemoveRange(assignment.Submissions);
        dbContext.AssignmentQuestions.RemoveRange(assignment.Questions);
        dbContext.Assignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
