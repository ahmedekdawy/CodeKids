using CodeKids.Application.Features.Analytics;

using CodeKids.Application.Features.Dashboard;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class DashboardEndpoints

{

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/dashboard/parent", async (

            HttpContext httpContext,

            IQueryHandler<GetParentDashboardQuery, ParentDashboardDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetParentDashboardQuery(userId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        app.MapGet("/api/dashboard/teacher", async (

            HttpContext httpContext,

            IQueryHandler<GetTeacherDashboardQuery, TeacherDashboardDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetTeacherDashboardQuery(userId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/dashboard/teacher/students/{studentId:guid}", async (

            Guid studentId,

            HttpContext httpContext,

            IQueryHandler<GetTeacherStudentDetailQuery, TeacherStudentDetailDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new GetTeacherStudentDetailQuery(userId, studentId),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/dashboard/teacher/classrooms/{classroomId:guid}/diagnosis", async (

            Guid classroomId,

            HttpContext httpContext,

            IQueryHandler<GetClassroomDiagnosisQuery, ClassroomDiagnosisDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new GetClassroomDiagnosisQuery(userId, classroomId),

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
