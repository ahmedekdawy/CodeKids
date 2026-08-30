using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class UpdateClassroomZoomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateClassroomZoomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateClassroomZoomCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var isSuperAdmin = string.Equals(command.ActorRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        if (!isSuperAdmin && !CreateClassroomCommandHandler.HasTeacher(classroom, command.ActorUserId))
        {
            throw new InvalidOperationException("Only an assigned classroom teacher can update Zoom links.");
        }

        classroom.ZoomLinksJson = ClassroomZoomLinks.Serialize(ClassroomZoomLinks.Normalize(command.ZoomLinks));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}
