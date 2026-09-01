using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class PublishAssignmentCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<PublishAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(PublishAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        await AssignmentAuthorization.EnsureCanManageClassroomAsync(
            dbContext, command.TeacherUserId, assignment.ClassroomId, "edit", cancellationToken);

        var wasPublished = assignment.IsPublished;
        assignment.IsPublished = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!wasPublished)
        {
            await notifications.NotifyAssignmentCreatedAsync(assignment, cancellationToken);
        }

        return (await CreateAssignmentCommandHandler.LoadAssignment(
            dbContext, assignment.Id, includeAnswerKey: true, cancellationToken))!;
    }
}
