using CodeKids.Application.Features.Attendance;
using CodeKids.Application.Features.StudentAttendance;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodeKids.Api;

public static class AttendanceEndpoints

{

    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/session-attendance", async (

            Guid? teacherId,

            int? grade,

            DateOnly? sessionDate,

            DateOnly? fromDate,

            DateOnly? toDate,

            IQueryHandler<ListTeacherSessionAttendanceQuery, IReadOnlyList<TeacherSessionAttendanceDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new ListTeacherSessionAttendanceQuery(teacherId, grade, sessionDate, fromDate, toDate),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/session-attendance", async (

            CreateTeacherSessionAttendanceRequest request,

            ICommandHandler<CreateTeacherSessionAttendanceCommand, TeacherSessionAttendanceDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateTeacherSessionAttendanceCommand(request.TeacherId, request.CourseId, request.SessionDate),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/session-attendance/{attendanceId:guid}", async (

            Guid attendanceId,

            ICommandHandler<DeleteTeacherSessionAttendanceCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteTeacherSessionAttendanceCommand(attendanceId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/session-attendance", async (

            HttpContext httpContext,

            int? grade,

            DateOnly? sessionDate,

            DateOnly? fromDate,

            DateOnly? toDate,

            IQueryHandler<ListTeacherSessionAttendanceQuery, IReadOnlyList<TeacherSessionAttendanceDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var teacherId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new ListTeacherSessionAttendanceQuery(teacherId, grade, sessionDate, fromDate, toDate),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/session-attendance", async (

            HttpContext httpContext,

            CreateMyTeacherSessionAttendanceRequest request,

            ICommandHandler<CreateTeacherSessionAttendanceCommand, TeacherSessionAttendanceDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var teacherId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateTeacherSessionAttendanceCommand(teacherId, request.CourseId, request.SessionDate),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/session-attendance/{attendanceId:guid}", async (

            Guid attendanceId,

            HttpContext httpContext,

            ICommandHandler<DeleteTeacherSessionAttendanceCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var teacherId = CurrentUser.GetUserId(httpContext.User);

                await handler.Handle(new DeleteTeacherSessionAttendanceCommand(attendanceId, teacherId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/admin/payroll-report", async (

            DateOnly fromDate,

            DateOnly toDate,

            Guid? teacherId,

            int? stage,

            int? grade,

            IQueryHandler<GetTeacherPayrollReportQuery, TeacherPayrollReportDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new GetTeacherPayrollReportQuery(fromDate, toDate, teacherId, stage, grade),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/admin/payroll-adjustments", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            Guid? teacherId,
            IQueryHandler<ListTeacherPayrollAdjustmentsQuery, IReadOnlyList<TeacherPayrollAdjustmentDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new ListTeacherPayrollAdjustmentsQuery(fromDate, toDate, teacherId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/payroll-adjustments", async (
            CreateTeacherPayrollAdjustmentRequest request,
            ICommandHandler<CreateTeacherPayrollAdjustmentCommand, TeacherPayrollAdjustmentDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new CreateTeacherPayrollAdjustmentCommand(
                        request.TeacherId,
                        request.Amount,
                        request.AdjustmentDate,
                        request.Notes),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/payroll-adjustments/{adjustmentId:guid}", async (
            Guid adjustmentId,
            ICommandHandler<DeleteTeacherPayrollAdjustmentCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.Handle(new DeleteTeacherPayrollAdjustmentCommand(adjustmentId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/admin/student-attendance", async (
            HttpContext httpContext,
            Guid? classroomId,
            int? gradeId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? studentSearch,
            string? sortKey,
            string? sortDir,
            int? page,
            int? pageSize,
            IQueryHandler<ListStudentClassroomAttendanceQuery, PagedStudentClassroomAttendanceResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                return Results.Ok(await handler.Handle(
                    new ListStudentClassroomAttendanceQuery(
                        userId,
                        role,
                        classroomId,
                        gradeId,
                        fromDate,
                        toDate,
                        studentSearch,
                        sortKey ?? "attendanceDate",
                        sortDir ?? "desc",
                        page ?? 1,
                        pageSize ?? 10),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/student-attendance", async (
            HttpContext httpContext,
            CreateStudentClassroomAttendanceRequest request,
            ICommandHandler<CreateStudentClassroomAttendanceCommand, StudentClassroomAttendanceDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var adminId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new CreateStudentClassroomAttendanceCommand(
                        adminId,
                        request.StudentId,
                        request.ClassroomId,
                        request.AttendanceDate,
                        request.Status,
                        IsAdmin: true),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/student-attendance/{attendanceId:guid}", async (
            Guid attendanceId,
            ICommandHandler<DeleteStudentClassroomAttendanceCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.Handle(new DeleteStudentClassroomAttendanceCommand(attendanceId, null, true), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/api/student-attendance", async (
            HttpContext httpContext,
            Guid? classroomId,
            int? gradeId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? studentSearch,
            string? sortKey,
            string? sortDir,
            int? page,
            int? pageSize,
            IQueryHandler<ListStudentClassroomAttendanceQuery, PagedStudentClassroomAttendanceResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                return Results.Ok(await handler.Handle(
                    new ListStudentClassroomAttendanceQuery(
                        userId,
                        role,
                        classroomId,
                        gradeId,
                        fromDate,
                        toDate,
                        studentSearch,
                        sortKey ?? "attendanceDate",
                        sortDir ?? "desc",
                        page ?? 1,
                        pageSize ?? 10),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/student-attendance", async (
            HttpContext httpContext,
            CreateMyStudentClassroomAttendanceRequest request,
            ICommandHandler<CreateStudentClassroomAttendanceCommand, StudentClassroomAttendanceDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new CreateStudentClassroomAttendanceCommand(
                        teacherId,
                        request.StudentId,
                        request.ClassroomId,
                        request.AttendanceDate,
                        request.Status,
                        IsAdmin: false),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/student-attendance/{attendanceId:guid}", async (
            Guid attendanceId,
            HttpContext httpContext,
            ICommandHandler<DeleteStudentClassroomAttendanceCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteStudentClassroomAttendanceCommand(attendanceId, teacherId, false), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        return app;

    }

}
