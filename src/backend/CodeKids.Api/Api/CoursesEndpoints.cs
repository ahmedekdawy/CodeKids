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

            return Results.Ok(await handler.Handle(new GetCoursesQuery(userId, role), cancellationToken));

        }).RequireAuthorization();

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

                        request.SortOrder,

                        request.SchoolType),

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

                        request.SortOrder,

                        request.SchoolType),

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
