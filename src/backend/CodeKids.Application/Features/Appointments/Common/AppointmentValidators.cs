using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

internal static class AppointmentValidators
{
    public static async Task ValidateAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid courseId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        var teacher = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher not found.");

        if (teacher.Role != UserRole.Teacher)
        {
            throw new InvalidOperationException("Selected user must be a teacher.");
        }

        _ = await dbContext.Courses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var start = startsAtUtc.ToUniversalTime();
        var end = endsAtUtc.ToUniversalTime();
        var overlapQuery = dbContext.Appointments.AsNoTracking()
            .Where(x => x.TeacherId == teacherId && x.StartsAtUtc < end && x.EndsAtUtc > start);

        if (excludeAppointmentId.HasValue)
        {
            overlapQuery = overlapQuery.Where(x => x.Id != excludeAppointmentId.Value);
        }

        if (await overlapQuery.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Teacher already has an appointment in this time slot.");
        }
    }

    public static async Task<AppointmentDto> LoadDtoAsync(
        IAppDbContext dbContext,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .FirstAsync(x => x.Id == appointmentId, cancellationToken);

        return ListAppointmentsQueryHandler.ToDto(appointment);
    }
}
