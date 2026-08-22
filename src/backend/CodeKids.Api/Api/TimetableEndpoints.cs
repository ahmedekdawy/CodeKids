using CodeKids.Application.Features.Timetable;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class TimetableEndpoints

{

    public static IEndpointRouteBuilder MapTimetableEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/timetable-entries", async (

            Guid? teacherId,

            int? grade,

            string? period,

            IQueryHandler<ListFixedTimetableEntriesQuery, IReadOnlyList<FixedTimetableEntryDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new ListFixedTimetableEntriesQuery(teacherId, grade, TimetablePeriodParser.ParseOptional(period)),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/timetable-entries", async (

            CreateFixedTimetableEntryRequest request,

            ICommandHandler<CreateFixedTimetableEntryCommand, FixedTimetableEntryDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateFixedTimetableEntryCommand(

                        request.TeacherId,

                        request.CourseId,

                        request.DayOfWeek,

                        request.SessionNumber,

                        TimetablePeriodParser.Parse(request.Period),

                        request.CombinedGrades),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/timetable-entries/{entryId:guid}", async (

            Guid entryId,

            UpdateFixedTimetableEntryRequest request,

            ICommandHandler<UpdateFixedTimetableEntryCommand, FixedTimetableEntryDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateFixedTimetableEntryCommand(

                        entryId,

                        request.TeacherId,

                        request.CourseId,

                        request.DayOfWeek,

                        request.SessionNumber,

                        TimetablePeriodParser.Parse(request.Period),

                        request.CombinedGrades),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/timetable-entries/{entryId:guid}", async (

            Guid entryId,

            ICommandHandler<DeleteFixedTimetableEntryCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteFixedTimetableEntryCommand(entryId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/timetable-entries", async (

            HttpContext httpContext,

            int? grade,

            string? period,

            IQueryHandler<ListFixedTimetableEntriesQuery, IReadOnlyList<FixedTimetableEntryDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var teacherId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new ListFixedTimetableEntriesQuery(teacherId, grade, TimetablePeriodParser.ParseOptional(period)),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        return app;

    }

}
