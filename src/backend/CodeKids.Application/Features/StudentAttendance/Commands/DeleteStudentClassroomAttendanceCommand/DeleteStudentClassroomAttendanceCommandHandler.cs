using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed class DeleteStudentClassroomAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteStudentClassroomAttendanceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteStudentClassroomAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.StudentClassroomAttendances
            .FirstOrDefaultAsync(x => x.Id == command.AttendanceId, cancellationToken)
            ?? throw new InvalidOperationException("Student classroom attendance not found.");

        if (!command.IsAdmin)
        {
            if (command.TeacherId is null)
            {
                throw new InvalidOperationException("You can only remove your own attendance records.");
            }

            await StudentClassroomAttendanceAccess.EnsureTeacherOwnsClassroomAsync(
                dbContext,
                command.TeacherId.Value,
                row.ClassroomId,
                cancellationToken);
        }

        dbContext.StudentClassroomAttendances.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
