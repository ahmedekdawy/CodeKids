using CodeKids.Application.Features.WeeklyReports;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class WeeklyReportsEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyReportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weekly-reports/grid", async (
            HttpContext httpContext,
            DateOnly weekStart,
            int? grade,
            IQueryHandler<GetWeeklyReportGridQuery, IReadOnlyList<StudentWeeklyReportGridRowDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetWeeklyReportGridQuery(teacherId, weekStart, grade),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/weekly-reports", async (
            HttpContext httpContext,
            int? grade,
            DateOnly? fromDate,
            DateOnly? toDate,
            IQueryHandler<ListStudentWeeklyReportsQuery, IReadOnlyList<StudentWeeklyReportDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new ListStudentWeeklyReportsQuery(teacherId, grade, fromDate, toDate),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPut("/api/weekly-reports", async (
            HttpContext httpContext,
            SaveWeeklyReportsRequest request,
            ICommandHandler<SaveWeeklyReportsCommand, IReadOnlyList<StudentWeeklyReportGridRowDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new SaveWeeklyReportsCommand(teacherId, request.WeekStartDate, request.Entries),
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
