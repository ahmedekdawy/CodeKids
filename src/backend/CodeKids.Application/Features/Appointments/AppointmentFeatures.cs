using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

public sealed record AppointmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Notes,
    string Label);

public sealed record CreateAppointmentRequest(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes,
    bool RepeatWeekly = false,
    DateTimeOffset? RepeatUntilUtc = null);

public sealed record UpdateAppointmentRequest(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes);

public sealed record ListAppointmentsQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    Guid? TeacherId = null) : IQuery<IReadOnlyList<AppointmentDto>>;

public sealed record CreateAppointmentCommand(
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes,
    bool RepeatWeekly = false,
    DateTimeOffset? RepeatUntilUtc = null) : ICommand<CreateAppointmentsResult>;

public sealed record CreateAppointmentsResult(IReadOnlyList<AppointmentDto> Items);

public sealed record UpdateAppointmentCommand(
    Guid AppointmentId,
    Guid TeacherId,
    Guid CourseId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Notes) : ICommand<AppointmentDto>;

public sealed record DeleteAppointmentCommand(Guid AppointmentId) : ICommand<bool>;

public sealed class ListAppointmentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    public async Task<IReadOnlyList<AppointmentDto>> Handle(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var appointments = dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .AsQueryable();

        if (query.FromUtc.HasValue)
        {
            appointments = appointments.Where(x => x.EndsAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            appointments = appointments.Where(x => x.StartsAtUtc <= query.ToUtc.Value);
        }

        if (query.TeacherId.HasValue)
        {
            appointments = appointments.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        return (await appointments
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken))
            .Select(ToDto)
            .ToList();
    }

    internal static AppointmentDto ToDto(Appointment appointment)
    {
        var teacherName = appointment.Teacher?.DisplayName ?? string.Empty;
        var courseName = appointment.Course?.Title ?? string.Empty;
        var courseGrade = appointment.Course?.Grade;
        var gradeLabel = FormatGradeLabel(courseGrade);
        var label = string.Join('-', new[] { teacherName, gradeLabel, courseName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new AppointmentDto(
            appointment.Id,
            appointment.TeacherId,
            teacherName,
            appointment.CourseId,
            courseName,
            courseGrade,
            appointment.StartsAtUtc,
            appointment.EndsAtUtc,
            appointment.Notes,
            label);
    }

    private static string FormatGradeLabel(int? grade) =>
        grade switch
        {
            null => "All",
            -1 => "KG1",
            0 => "KG2",
            _ => $"Grade {grade}"
        };
}

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
