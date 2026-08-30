using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class UpdateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomCommand command, CancellationToken cancellationToken)
    {
        var grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        var assignments = await CreateClassroomCommandHandler.ValidateCourseAssignments(
            dbContext, command.Courses, grade, cancellationToken);

        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        classroom.Name = name;
        classroom.Description = (command.Description ?? string.Empty).Trim();
        classroom.Grade = grade;
        classroom.CourseId = assignments.Count > 0 ? assignments[0].CourseId : null;
        classroom.WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim();
        classroom.ZoomMeetingLink = (command.ZoomMeetingLink ?? string.Empty).Trim();
        classroom.WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim();

        await CreateClassroomCommandHandler.ReplaceCourseAssignmentsAsync(
            dbContext, classroom.Id, assignments, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}
