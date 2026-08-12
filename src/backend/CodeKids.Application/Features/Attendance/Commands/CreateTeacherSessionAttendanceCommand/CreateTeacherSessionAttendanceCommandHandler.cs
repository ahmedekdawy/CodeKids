using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed class CreateTeacherSessionAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateTeacherSessionAttendanceCommand, TeacherSessionAttendanceDto>
{
    public async Task<TeacherSessionAttendanceDto> Handle(
        CreateTeacherSessionAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        await TeacherSessionAttendanceValidators.ValidateAsync(
            dbContext,
            command.TeacherId,
            command.CourseId,
            command.SessionDate,
            cancellationToken);

        var row = new TeacherSessionAttendance
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            CourseId = command.CourseId,
            SessionDate = command.SessionDate,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.TeacherSessionAttendances.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await TeacherSessionAttendanceValidators.LoadDtoAsync(dbContext, row.Id, cancellationToken);
    }
}
