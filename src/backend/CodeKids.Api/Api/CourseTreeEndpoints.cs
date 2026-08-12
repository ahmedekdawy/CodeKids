using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class CourseTreeEndpoints
{
    public static IEndpointRouteBuilder MapCourseTreeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/courses/{courseId:guid}/units", async (
            Guid courseId,
            CreateCourseUnitRequest request,
            ICommandHandler<CreateCourseUnitCommand, CourseUnitDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
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
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/units/{unitId:guid}", async (
            Guid unitId,
            UpdateCourseUnitRequest request,
            ICommandHandler<UpdateCourseUnitCommand, CourseUnitDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
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
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/units/{unitId:guid}", async (
            Guid unitId,
            ICommandHandler<DeleteCourseUnitCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.Handle(new DeleteCourseUnitCommand(unitId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/units/{unitId:guid}/lessons", async (
            Guid unitId,
            CreateCourseLessonRequest request,
            ICommandHandler<CreateCourseLessonCommand, CourseLessonDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
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
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/lessons/{lessonId:guid}", async (
            Guid lessonId,
            UpdateCourseLessonRequest request,
            ICommandHandler<UpdateCourseLessonCommand, CourseLessonDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
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
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/lessons/{lessonId:guid}", async (
            Guid lessonId,
            ICommandHandler<DeleteCourseLessonCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.Handle(new DeleteCourseLessonCommand(lessonId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        return app;
    }
}
