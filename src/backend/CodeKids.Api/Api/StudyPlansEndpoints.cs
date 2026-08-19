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
