using CodeKids.Application.Features.Assignments;

using CodeKids.Application.Features.Media;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class AssignmentsEndpoints

{

    public static IEndpointRouteBuilder MapAssignmentsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/assignments", async (

            Guid? classroomId,

            HttpContext httpContext,

            IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(new GetAssignmentsQuery(userId, role, classroomId), cancellationToken));

        }).RequireAuthorization();

        app.MapGet("/api/assignments/{assignmentId:guid}", async (

            Guid assignmentId,

            HttpContext httpContext,

            IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var assignment = await handler.Handle(new GetAssignmentByIdQuery(assignmentId, userId, role), cancellationToken);

            return assignment is null ? Results.NotFound() : Results.Ok(assignment);

        }).RequireAuthorization();

        app.MapPost("/api/assignments", async (

            CreateAssignmentRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateAssignmentCommand, AssignmentDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateAssignmentCommand(

                        userId,

                        request.ClassroomId,

                        request.Title,

                        request.Description,

                        request.DueAtUtc,

                        request.XpReward,

                        request.IsPublished,

                        request.Questions),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPut("/api/assignments/{assignmentId:guid}", async (
            Guid assignmentId,
            UpdateAssignmentRequest request,
            HttpContext httpContext,
            ICommandHandler<UpdateAssignmentCommand, AssignmentDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new UpdateAssignmentCommand(
                        userId,
                        assignmentId,
                        request.ClassroomId,
                        request.Title,
                        request.Description,
                        request.DueAtUtc,
                        request.XpReward,
                        request.IsPublished,
                        request.Questions),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/assignments/{assignmentId:guid}/publish", async (
            Guid assignmentId,
            HttpContext httpContext,
            ICommandHandler<PublishAssignmentCommand, AssignmentDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new PublishAssignmentCommand(userId, assignmentId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapDelete("/api/assignments/{assignmentId:guid}", async (
            Guid assignmentId,
            HttpContext httpContext,
            ICommandHandler<DeleteAssignmentCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteAssignmentCommand(userId, assignmentId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/assignments/submit", async (

            SubmitAssignmentRequest request,

            HttpContext httpContext,

            ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new SubmitAssignmentCommand(userId, request.AssignmentId, request.Answers),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/assignments/{assignmentId:guid}/submissions", async (

            Guid assignmentId,

            HttpContext httpContext,

            IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetAssignmentSubmissionsQuery(userId, assignmentId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/assignments/submissions/grade", async (

            GradeSubmissionRequest request,

            HttpContext httpContext,

            ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new GradeSubmissionCommand(userId, request.SubmissionId, request.TeacherFeedback, request.FeedbackImageMediaAssetId, request.Answers),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/assignments/{assignmentId:guid}/solution-video", async (

            Guid assignmentId,

            AttachLessonVideoRequest request,

            HttpContext httpContext,

            ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new AttachAssignmentSolutionVideoCommand(userId, assignmentId, request.MediaAssetId),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/assignments/{assignmentId:guid}/solution-video", async (

            Guid assignmentId,

            HttpContext httpContext,

            ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                await handler.Handle(new DeleteAssignmentSolutionVideoCommand(userId, assignmentId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        return app;

    }

}
