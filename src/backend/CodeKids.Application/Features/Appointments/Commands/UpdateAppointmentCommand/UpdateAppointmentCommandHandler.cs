using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed class UpdateAppointmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateAppointmentCommand, AppointmentDto>
{
    public async Task<AppointmentDto> Handle(UpdateAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        await AppointmentValidators.ValidateAsync(
            dbContext,
            command.TeacherId,
            command.CourseId,
            command.StartsAtUtc,
            command.EndsAtUtc,
            excludeAppointmentId: command.AppointmentId,
            cancellationToken);

        appointment.TeacherId = command.TeacherId;
        appointment.CourseId = command.CourseId;
        appointment.StartsAtUtc = command.StartsAtUtc.ToUniversalTime();
        appointment.EndsAtUtc = command.EndsAtUtc.ToUniversalTime();
        appointment.Notes = (command.Notes ?? string.Empty).Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return await AppointmentValidators.LoadDtoAsync(dbContext, appointment.Id, cancellationToken);
    }
}
