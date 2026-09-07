using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class UpdateAssignmentCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<UpdateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(UpdateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .Include(x => x.Submissions)
                .ThenInclude(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        await AssignmentAuthorization.EnsureCanManageClassroomAsync(
            dbContext, command.TeacherUserId, assignment.ClassroomId, "edit", cancellationToken);

        if (command.ClassroomId != assignment.ClassroomId)
        {
            var classroomExists = await dbContext.Classrooms.AnyAsync(x => x.Id == command.ClassroomId, cancellationToken);
            if (!classroomExists)
            {
                throw new InvalidOperationException("Classroom not found.");
            }

            await AssignmentAuthorization.EnsureCanManageClassroomAsync(
                dbContext, command.TeacherUserId, command.ClassroomId, "edit", cancellationToken);
            assignment.ClassroomId = command.ClassroomId;
        }

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Assignment title is required.");
        }

        if (command.Questions is null || command.Questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one question.");
        }

        assignment.Title = title;
        assignment.Description = (command.Description ?? string.Empty).Trim();
        assignment.DueAtUtc = command.DueAtUtc?.ToUniversalTime();
        assignment.XpReward = Math.Max(0, command.XpReward);
        var wasPublished = assignment.IsPublished;
        assignment.IsPublished = command.IsPublished;

        await AssignmentQuestionSync.ApplyAsync(dbContext, assignment, command.Questions, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        if (!wasPublished && assignment.IsPublished)
        {
            await notifications.NotifyAssignmentCreatedAsync(assignment, cancellationToken);
        }
        return (await CreateAssignmentCommandHandler.LoadAssignment(
            dbContext, assignment.Id, includeAnswerKey: true, cancellationToken))!;
    }
}
