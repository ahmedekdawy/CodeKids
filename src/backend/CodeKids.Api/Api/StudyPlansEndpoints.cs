using CodeKids.Application.Features.StudyPlans;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodeKids.Api;

public static class StudyPlansEndpoints
{
    public static IEndpointRouteBuilder MapStudyPlansEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/study-plans", async (
            HttpContext httpContext,
            Guid? courseId,
            Guid? teacherId,
            Guid? studentId,
            DateOnly? fromDate,
            DateOnly? toDate,
            IQueryHandler<ListWeeklyStudyPlansQuery, IReadOnlyList<WeeklyStudyPlanDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                return Results.Ok(await handler.Handle(
                    new ListWeeklyStudyPlansQuery(userId, role, teacherId, courseId, studentId, fromDate, toDate),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,Student,Parent,SuperAdmin" });

        app.MapGet("/api/admin/study-plans", async (
            Guid? teacherId,
            Guid? courseId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? sortKey,
            string? sortDir,
            int? page,
            int? pageSize,
            IQueryHandler<ListAdminWeeklyStudyPlansQuery, PagedWeeklyStudyPlansResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(
                new ListAdminWeeklyStudyPlansQuery(
                    teacherId,
                    courseId,
                    fromDate,
                    toDate,
                    sortKey ?? "fromDate",
                    sortDir ?? "desc",
                    page ?? 1,
                    pageSize ?? 10),
                cancellationToken));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/study-plans", async (
            HttpContext httpContext,
            SaveWeeklyStudyPlanRequest request,
            ICommandHandler<SaveWeeklyStudyPlanCommand, WeeklyStudyPlanDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new SaveWeeklyStudyPlanCommand(
                        teacherId,
                        request.Id,
                        request.CourseId,
                        request.FromDate,
                        request.ToDate,
                        request.Notes,
                        request.Weeks ?? []),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/study-plans/generate", async (
            HttpContext httpContext,
            GenerateWeeklyStudyPlanRequest request,
            ICommandHandler<GenerateWeeklyStudyPlanCommand, GenerateWeeklyStudyPlanResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GenerateWeeklyStudyPlanCommand(
                        teacherId,
                        request.CourseId,
                        request.FromDate,
                        request.ToDate,
                        request.Language,
                        request.Prompt),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/study-plans/{planId:guid}", async (
            HttpContext httpContext,
            Guid planId,
            ICommandHandler<DeleteWeeklyStudyPlanCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteWeeklyStudyPlanCommand(teacherId, planId), cancellationToken);
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
