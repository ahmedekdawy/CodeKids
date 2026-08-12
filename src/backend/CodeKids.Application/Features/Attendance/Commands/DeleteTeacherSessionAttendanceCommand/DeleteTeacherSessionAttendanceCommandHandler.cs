using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed class DeleteTeacherSessionAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteTeacherSessionAttendanceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteTeacherSessionAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TeacherSessionAttendances
            .FirstOrDefaultAsync(x => x.Id == command.AttendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Session attendance not found.");

        if (command.ActingTeacherId.HasValue && row.TeacherId != command.ActingTeacherId.Value)
        {
            throw new InvalidOperationException("You can only remove your own attendance records.");
        }

        dbContext.TeacherSessionAttendances.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
