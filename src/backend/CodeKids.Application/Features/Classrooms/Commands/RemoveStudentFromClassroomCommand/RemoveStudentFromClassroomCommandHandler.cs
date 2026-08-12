using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class RemoveStudentFromClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(RemoveStudentFromClassroomCommand command, CancellationToken cancellationToken)
    {
        var membership = await dbContext.ClassroomStudents.FirstOrDefaultAsync(
            x => x.ClassroomId == command.ClassroomId && x.StudentId == command.StudentId,
            cancellationToken)
            ?? throw new InvalidOperationException("Student is not enrolled in this classroom.");

        dbContext.ClassroomStudents.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, command.ClassroomId, cancellationToken))!;
    }
}
