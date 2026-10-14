using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public sealed record AvailabilityPeriodDto(int DayOfWeek, int StartMinutes, int EndMinutes);

public sealed record SaveAvailabilityRequest(IReadOnlyList<AvailabilityPeriodDto>? Periods);

public sealed record TeacherVacationDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Note);

public sealed record CreateTeacherVacationRequest(Guid? TeacherId, DateOnly StartDate, DateOnly EndDate, string? Note);

public static class AvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/availability/{userId:guid}", async (
            Guid userId,
            HttpContext httpContext,
            IAppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanAccessAsync(dbContext, httpContext, userId, write: false, cancellationToken);
                var periods = await dbContext.UserAvailabilities.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .OrderBy(x => x.DayOfWeek)
                    .ThenBy(x => x.StartMinutes)
                    .Select(x => new AvailabilityPeriodDto(x.DayOfWeek, x.StartMinutes, x.EndMinutes))
                    .ToListAsync(cancellationToken);
                return Results.Ok(periods);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,Student,SuperAdmin" });

        // Replaces the user's whole weekly availability with the periods sent.
        app.MapPut("/api/availability/{userId:guid}", async (
            Guid userId,
            SaveAvailabilityRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanAccessAsync(dbContext, httpContext, userId, write: true, cancellationToken);
                var periods = ValidatePeriods(request.Periods ?? []);

                var existing = await dbContext.UserAvailabilities
                    .Where(x => x.UserId == userId)
                    .ToListAsync(cancellationToken);
                dbContext.UserAvailabilities.RemoveRange(existing);
                dbContext.UserAvailabilities.AddRange(periods.Select(x => new UserAvailability
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DayOfWeek = x.DayOfWeek,
                    StartMinutes = x.StartMinutes,
                    EndMinutes = x.EndMinutes
                }));
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(periods);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,Student,SuperAdmin" });

        // Admins see every teacher's vacations (or one teacher's); a teacher sees only their own.
        app.MapGet("/api/teacher-vacations", async (
            Guid? teacherId,
            HttpContext httpContext,
            IAppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var query = dbContext.TeacherVacations.AsNoTracking();
            var scopedTeacherId = IsAdmin(httpContext) ? teacherId : CurrentUser.GetUserId(httpContext.User);
            if (scopedTeacherId is Guid id)
            {
                query = query.Where(x => x.TeacherId == id);
            }

            var vacations = await query
                .OrderByDescending(x => x.StartDate)
                .Select(x => new TeacherVacationDto(
                    x.Id,
                    x.TeacherId,
                    x.Teacher!.DisplayName,
                    x.StartDate,
                    x.EndDate,
                    x.Note))
                .ToListAsync(cancellationToken);
            return Results.Ok(vacations);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/teacher-vacations", async (
            CreateTeacherVacationRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var callerId = CurrentUser.GetUserId(httpContext.User);
                var teacherId = IsAdmin(httpContext)
                    ? request.TeacherId ?? throw new InvalidOperationException("Teacher is required.")
                    : callerId;

                var teacher = await dbContext.Users.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == teacherId && x.Role == UserRole.Teacher, cancellationToken)
                    ?? throw new InvalidOperationException("Teacher not found.");

                if (request.EndDate < request.StartDate)
                {
                    throw new InvalidOperationException("Vacation end date must be on or after the start date.");
                }

                var note = (request.Note ?? string.Empty).Trim();
                if (note.Length > TeacherVacation.MaxNoteLength)
                {
                    throw new InvalidOperationException($"Vacation note must be {TeacherVacation.MaxNoteLength} characters or fewer.");
                }

                var overlaps = await dbContext.TeacherVacations.AnyAsync(
                    x => x.TeacherId == teacherId && x.StartDate <= request.EndDate && x.EndDate >= request.StartDate,
                    cancellationToken);
                if (overlaps)
                {
                    throw new InvalidOperationException("This vacation overlaps one the teacher already has.");
                }

                var vacation = new TeacherVacation
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Note = note
                };
                dbContext.TeacherVacations.Add(vacation);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(new TeacherVacationDto(
                    vacation.Id,
                    teacherId,
                    teacher.DisplayName,
                    vacation.StartDate,
                    vacation.EndDate,
                    vacation.Note));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapDelete("/api/teacher-vacations/{vacationId:guid}", async (
            Guid vacationId,
            HttpContext httpContext,
            IAppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var vacation = await dbContext.TeacherVacations
                .FirstOrDefaultAsync(x => x.Id == vacationId, cancellationToken);
            if (vacation is null
                || (!IsAdmin(httpContext) && vacation.TeacherId != CurrentUser.GetUserId(httpContext.User)))
            {
                return Results.NotFound();
            }

            dbContext.TeacherVacations.Remove(vacation);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        return app;
    }

    private static bool IsAdmin(HttpContext httpContext) =>
        httpContext.User.IsInRole(nameof(UserRole.SuperAdmin));

    /// <summary>
    /// Admins can read and write any teacher's or student's availability. Teachers and students can write
    /// only their own; a teacher can also read a student's.
    /// </summary>
    private static async Task EnsureCanAccessAsync(
        IAppDbContext dbContext,
        HttpContext httpContext,
        Guid userId,
        bool write,
        CancellationToken cancellationToken)
    {
        var target = await dbContext.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.Role })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("User not found.");
        if (target.Role is not (UserRole.Teacher or UserRole.Student))
        {
            throw new InvalidOperationException("Availability applies to teachers and students only.");
        }

        if (IsAdmin(httpContext) || CurrentUser.GetUserId(httpContext.User) == userId)
        {
            return;
        }

        var teacherReadingStudent = !write
            && target.Role == UserRole.Student
            && httpContext.User.IsInRole(nameof(UserRole.Teacher));
        if (!teacherReadingStudent)
        {
            throw new InvalidOperationException("You can only manage your own availability.");
        }
    }

    private static List<AvailabilityPeriodDto> ValidatePeriods(IReadOnlyList<AvailabilityPeriodDto> periods)
    {
        var ordered = periods
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartMinutes)
            .ToList();

        foreach (var period in ordered)
        {
            if (period.DayOfWeek is < 0 or > 6)
            {
                throw new InvalidOperationException("Day of week must be between 0 (Sunday) and 6 (Saturday).");
            }

            if (period.StartMinutes < 0
                || period.EndMinutes > UserAvailability.MinutesPerDay
                || period.StartMinutes >= period.EndMinutes)
            {
                throw new InvalidOperationException("Each availability period must end after it starts, within one day.");
            }
        }

        foreach (var day in ordered.GroupBy(x => x.DayOfWeek))
        {
            var dayPeriods = day.ToList();
            if (dayPeriods.Count > UserAvailability.MaxPeriodsPerDay)
            {
                throw new InvalidOperationException($"A day can have at most {UserAvailability.MaxPeriodsPerDay} availability periods.");
            }

            for (var i = 1; i < dayPeriods.Count; i++)
            {
                if (dayPeriods[i].StartMinutes < dayPeriods[i - 1].EndMinutes)
                {
                    throw new InvalidOperationException("Availability periods on the same day cannot overlap.");
                }
            }
        }

        return ordered;
    }
}
