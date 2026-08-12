using CodeKids.Application.Features.Appointments;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AppointmentsEndpoints

{

    public static IEndpointRouteBuilder MapAppointmentsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/appointments", async (

            DateTimeOffset? fromUtc,

            DateTimeOffset? toUtc,

            IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(new ListAppointmentsQuery(fromUtc, toUtc), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/appointments", async (

            CreateAppointmentRequest request,

            ICommandHandler<CreateAppointmentCommand, CreateAppointmentsResult> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateAppointmentCommand(

                        request.TeacherId,

                        request.CourseId,

                        request.StartsAtUtc,

                        request.EndsAtUtc,

                        request.Notes,

                        request.RepeatWeekly,

                        request.RepeatUntilUtc),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/appointments/{appointmentId:guid}", async (

            Guid appointmentId,

            UpdateAppointmentRequest request,

            ICommandHandler<UpdateAppointmentCommand, AppointmentDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateAppointmentCommand(

                        appointmentId,

                        request.TeacherId,

                        request.CourseId,

                        request.StartsAtUtc,

                        request.EndsAtUtc,

                        request.Notes),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/appointments/{appointmentId:guid}", async (

            Guid appointmentId,

            ICommandHandler<DeleteAppointmentCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteAppointmentCommand(appointmentId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/appointments", async (

            HttpContext httpContext,

            DateTimeOffset? fromUtc,

            DateTimeOffset? toUtc,

            IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var teacherId = CurrentUser.GetUserId(httpContext.User);

            return Results.Ok(await handler.Handle(new ListAppointmentsQuery(fromUtc, toUtc, teacherId), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        return app;

    }

}
