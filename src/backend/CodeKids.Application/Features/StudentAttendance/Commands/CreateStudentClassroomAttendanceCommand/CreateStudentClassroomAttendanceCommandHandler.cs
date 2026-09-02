using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAttendance;

public sealed class CreateStudentClassroomAttendanceCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateStudentClassroomAttendanceCommand, StudentClassroomAttendanceDto>
{
    public async Task<StudentClassroomAttendanceDto> Handle(
        CreateStudentClassroomAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.AttendanceDate == default)
        {
            throw new InvalidOperationException("Student attendance date is required.");
        }

        var status = StudentClassroomAttendanceAccess.ParseStatus(command.Status);

        if (!command.IsAdmin)
        {
            await StudentClassroomAttendanceAccess.EnsureTeacherOwnsClassroomAsync(
                dbContext,
                command.RecordedByTeacherId,
                command.ClassroomId,
                cancellationToken);
        }

        await StudentClassroomAttendanceAccess.EnsureStudentInClassroomAsync(
            dbContext,
            command.StudentId,
            command.ClassroomId,
            cancellationToken);

        var existing = await dbContext.StudentClassroomAttendances
            .FirstOrDefaultAsync(
                x => x.StudentId == command.StudentId
                     && x.ClassroomId == command.ClassroomId
                     && x.AttendanceDate == command.AttendanceDate,
                cancellationToken);

        if (existing is not null)
        {
            existing.Status = status;
            existing.RecordedByTeacherId = command.RecordedByTeacherId;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await StudentClassroomAttendanceAccess.LoadDtoAsync(dbContext, existing.Id, cancellationToken);
        }

        var row = new StudentClassroomAttendance
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            ClassroomId = command.ClassroomId,
            AttendanceDate = command.AttendanceDate,
            Status = status,
            RecordedByTeacherId = command.RecordedByTeacherId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.StudentClassroomAttendances.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await StudentClassroomAttendanceAccess.LoadDtoAsync(dbContext, row.Id, cancellationToken);
    }
}
