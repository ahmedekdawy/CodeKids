using CodeKids.Application.Features.Admin;

using CodeKids.Application.Features.Courses;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class CoursesEndpoints

{

    public static IEndpointRouteBuilder MapCoursesEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/courses", async (

            HttpContext httpContext,

            IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>> handler,

            bool? includeContent,

            CancellationToken cancellationToken) =>

        {

            Guid? userId = null;

            string? role = null;

            try

            {

                userId = CurrentUser.GetUserId(httpContext.User);

                role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value

                    ?? httpContext.User.FindFirst("role")?.Value;

            }

            catch

            {

                // Authorized endpoint; ignore if claim missing.

            }

            return Results.Ok(await handler.Handle(new GetCoursesQuery(userId, role, includeContent ?? true), cancellationToken));

        }).RequireAuthorization();

        app.MapGet("/api/courses/{courseId:guid}", async (
            Guid courseId,
            HttpContext httpContext,
            IQueryHandler<GetCourseByIdQuery, CourseDto?> handler,
            CancellationToken cancellationToken) =>
        {
            Guid? userId = null;
            string? role = null;
            try
            {
                userId = CurrentUser.GetUserId(httpContext.User);
                role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? httpContext.User.FindFirst("role")?.Value;
            }
            catch
            {
                // Authorized endpoint; ignore if claim missing.
            }

            var course = await handler.Handle(new GetCourseByIdQuery(courseId, userId, role), cancellationToken);
            return course is null ? Results.NotFound() : Results.Ok(course);
        }).RequireAuthorization();

        app.MapGet("/api/admin/courses", async (

            string? titleSearch,

            int? stageId,

            int? grade,

            string? sortKey,

            string? sortDir,

            int? page,

            int? pageSize,

            IQueryHandler<ListAdminCoursesQuery, PagedCoursesResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(

                new ListAdminCoursesQuery(

                    titleSearch,

                    stageId,

                    grade,

                    sortKey ?? "title",

                    sortDir ?? "asc",

                    page ?? 1,

                    pageSize ?? 10),

                cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/courses", async (

            CreateCourseRequest request,

            ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateCourseCommand(

                        request.Title,

                        request.Theme,

                        request.Description,

                        request.AgeMin,

                        request.AgeMax,

                        request.Term,

                        request.Grades,

                        request.StageId,

                        request.SortOrder,

                        request.SchoolType,

                        request.IsPublished),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/courses/{courseId:guid}", async (

            Guid courseId,

            UpdateCourseRequest request,

            ICommandHandler<UpdateCourseCommand, CourseSummaryDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateCourseCommand(

                        courseId,

                        request.Title,

                        request.Theme,

                        request.Description,

                        request.AgeMin,

                        request.AgeMax,

                        request.Term,

                        request.Grade,

                        request.StageId,

                        request.SortOrder,

                        request.SchoolType,

                        request.IsPublished),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/courses/{courseId:guid}/published", async (

            Guid courseId,

            SetCoursePublishedRequest request,

            ICommandHandler<SetCoursePublishedCommand, CourseSummaryDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new SetCoursePublishedCommand(courseId, request.IsPublished),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/courses/{courseId:guid}", async (

            Guid courseId,

            ICommandHandler<DeleteCourseCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteCourseCommand(courseId), cancellationToken);

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
