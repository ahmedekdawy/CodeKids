using CodeKids.Application.Features.Exams;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class ExamsEndpoints

{

    public static IEndpointRouteBuilder MapExamsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/exams", async (

            Guid? classroomId,

            HttpContext httpContext,

            IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(new GetExamsQuery(userId, role, classroomId), cancellationToken));

        }).RequireAuthorization();

        app.MapGet("/api/exams/{examId:guid}", async (

            Guid examId,

            HttpContext httpContext,

            IQueryHandler<GetExamByIdQuery, ExamDto?> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var exam = await handler.Handle(new GetExamByIdQuery(examId, userId, role), cancellationToken);

            return exam is null ? Results.NotFound() : Results.Ok(exam);

        }).RequireAuthorization();

        app.MapPost("/api/exams", async (

            CreateExamRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateExamCommand, ExamDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateExamCommand(

                        userId,

                        request.ClassroomId,

                        request.CourseId,

                        request.Title,

                        request.Description,

                        request.DueAtUtc,

                        request.XpReward,

                        request.QuestionIds),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/exams/{examId:guid}/start", async (

            Guid examId,

            HttpContext httpContext,

            ICommandHandler<StartExamCommand, ExamAttemptDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new StartExamCommand(userId, examId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapPost("/api/exams/submit", async (

            SubmitExamRequest request,

            HttpContext httpContext,

            ICommandHandler<SubmitExamCommand, ExamAttemptDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new SubmitExamCommand(userId, request.ExamId, request.Answers),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/exams/{examId:guid}/attempts", async (

            Guid examId,

            HttpContext httpContext,

            IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new GetExamAttemptsQuery(userId, examId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/exams/attempts/grade", async (
            GradeExamAttemptRequest request,
            HttpContext httpContext,
            ICommandHandler<GradeExamAttemptCommand, ExamAttemptDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GradeExamAttemptCommand(userId, request.AttemptId, request.TeacherFeedback, request.FeedbackImageMediaAssetId, request.Answers),
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
