using System.Security.Claims;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class CourseTreeEndpoints
{
    private static readonly AuthorizeAttribute TeacherOrAdmin =
        new() { Roles = "Teacher,SuperAdmin" };

    public static IEndpointRouteBuilder MapCourseTreeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/courses/{courseId:guid}/units", async (
            Guid courseId,
            CreateCourseUnitRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<CreateCourseUnitCommand, CourseUnitDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageCourseAsync(httpContext, dbContext, courseId, cancellationToken);
                return Results.Ok(await handler.Handle(
                    new CreateCourseUnitCommand(
                        courseId,
                        request.Title,
                        request.Description,
                        request.SortOrder),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapPut("/api/admin/units/{unitId:guid}", async (
            Guid unitId,
            UpdateCourseUnitRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<UpdateCourseUnitCommand, CourseUnitDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageUnitAsync(httpContext, dbContext, unitId, cancellationToken);
                return Results.Ok(await handler.Handle(
                    new UpdateCourseUnitCommand(
                        unitId,
                        request.Title,
                        request.Description,
                        request.SortOrder),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapDelete("/api/admin/units/{unitId:guid}", async (
            Guid unitId,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<DeleteCourseUnitCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageUnitAsync(httpContext, dbContext, unitId, cancellationToken);
                await handler.Handle(new DeleteCourseUnitCommand(unitId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapPost("/api/admin/units/{unitId:guid}/lessons", async (
            Guid unitId,
            CreateCourseLessonRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<CreateCourseLessonCommand, CourseLessonDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageUnitAsync(httpContext, dbContext, unitId, cancellationToken);
                return Results.Ok(await handler.Handle(
                    new CreateCourseLessonCommand(
                        unitId,
                        request.Title,
                        request.Theme,
                        request.Description,
                        request.Difficulty,
                        request.XpReward,
                        request.SortOrder),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapPut("/api/admin/lessons/{lessonId:guid}", async (
            Guid lessonId,
            UpdateCourseLessonRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<UpdateCourseLessonCommand, CourseLessonDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageLessonAsync(httpContext, dbContext, lessonId, cancellationToken);
                return Results.Ok(await handler.Handle(
                    new UpdateCourseLessonCommand(
                        lessonId,
                        request.UnitId,
                        request.Title,
                        request.Theme,
                        request.Description,
                        request.Difficulty,
                        request.XpReward,
                        request.SortOrder),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapDelete("/api/admin/lessons/{lessonId:guid}", async (
            Guid lessonId,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<DeleteCourseLessonCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageLessonAsync(httpContext, dbContext, lessonId, cancellationToken);
                await handler.Handle(new DeleteCourseLessonCommand(lessonId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        return app;
    }

    private static (Guid UserId, string? Role) Actor(HttpContext httpContext)
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;
        return (userId, role);
    }

    private static Task EnsureCanManageCourseAsync(
        HttpContext httpContext,
        IAppDbContext dbContext,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var (userId, role) = Actor(httpContext);
        return CourseTreeAccess.EnsureCanManageCourseAsync(dbContext, userId, role, courseId, cancellationToken);
    }

    private static async Task EnsureCanManageUnitAsync(
        HttpContext httpContext,
        IAppDbContext dbContext,
        Guid unitId,
        CancellationToken cancellationToken)
    {
        var unit = await CourseOutlineResolver.FindUnitAsync(dbContext, unitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit not found.");
        await EnsureCanManageCourseAsync(httpContext, dbContext, unit.Course.Id, cancellationToken);
    }

    private static async Task EnsureCanManageLessonAsync(
        HttpContext httpContext,
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var lesson = await CourseOutlineResolver.FindLessonAsync(dbContext, lessonId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson not found.");
        await EnsureCanManageCourseAsync(httpContext, dbContext, lesson.Course.Id, cancellationToken);
    }
}
