using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed class DeleteAppointmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteAppointmentCommand, bool>
{
    public async Task<bool> Handle(DeleteAppointmentCommand command, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(x => x.Id == command.AppointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        dbContext.Appointments.Remove(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
