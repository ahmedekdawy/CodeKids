using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class UpdateClassroomAssignmentsCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomAssignmentsCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var assignments = await CreateClassroomCommandHandler.ValidateCourseAssignments(
            dbContext, command.Courses, classroom.Grade, cancellationToken);

        classroom.CourseId = assignments.Count > 0 ? assignments[0].CourseId : null;
        await CreateClassroomCommandHandler.ReplaceCourseAssignmentsAsync(
            dbContext, classroom.Id, assignments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}
