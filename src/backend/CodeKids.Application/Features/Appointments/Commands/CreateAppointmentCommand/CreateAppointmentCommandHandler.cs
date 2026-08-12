using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed class CreateAppointmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateAppointmentCommand, CreateAppointmentsResult>
{
    private const int MaxWeeklyOccurrences = 104;

    public async Task<CreateAppointmentsResult> Handle(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        var occurrences = BuildOccurrences(command);
        if (occurrences.Count > MaxWeeklyOccurrences)
        {
            throw new InvalidOperationException("Weekly recurrence exceeds the maximum number of sessions.");
        }

        foreach (var (startsAtUtc, endsAtUtc) in occurrences)
        {
            await AppointmentValidators.ValidateAsync(
                dbContext,
                command.TeacherId,
                command.CourseId,
                startsAtUtc,
                endsAtUtc,
                excludeAppointmentId: null,
                cancellationToken);
        }

        var notes = (command.Notes ?? string.Empty).Trim();
        var createdIds = new List<Guid>(occurrences.Count);
        foreach (var (startsAtUtc, endsAtUtc) in occurrences)
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                TeacherId = command.TeacherId,
                CourseId = command.CourseId,
                StartsAtUtc = startsAtUtc,
                EndsAtUtc = endsAtUtc,
                Notes = notes,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            dbContext.Appointments.Add(appointment);
            createdIds.Add(appointment.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var items = new List<AppointmentDto>(createdIds.Count);
        foreach (var id in createdIds)
        {
            items.Add(await AppointmentValidators.LoadDtoAsync(dbContext, id, cancellationToken));
        }

        return new CreateAppointmentsResult(items);
    }

    private static IReadOnlyList<(DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc)> BuildOccurrences(
        CreateAppointmentCommand command)
    {
        var start = command.StartsAtUtc.ToUniversalTime();
        var end = command.EndsAtUtc.ToUniversalTime();
        var duration = end - start;

        if (!command.RepeatWeekly)
        {
            return [(start, end)];
        }

        if (!command.RepeatUntilUtc.HasValue)
        {
            throw new InvalidOperationException("Repeat until date is required for weekly recurrence.");
        }

        var repeatUntil = command.RepeatUntilUtc.Value.ToUniversalTime();
        if (repeatUntil < start)
        {
            throw new InvalidOperationException("Repeat until date must be on or after the first session.");
        }

        var occurrences = new List<(DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc)>();
        var currentStart = start;
        while (currentStart <= repeatUntil)
        {
            occurrences.Add((currentStart, currentStart + duration));
            currentStart = currentStart.AddDays(7);
        }

        return occurrences;
    }
}
