using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Appointments;

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
