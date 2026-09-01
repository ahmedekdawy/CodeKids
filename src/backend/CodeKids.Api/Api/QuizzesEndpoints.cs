using CodeKids.Application.Features.Quizzes;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class QuizzesEndpoints

{

    public static IEndpointRouteBuilder MapQuizzesEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/quizzes", async (

            Guid? courseId,

            Guid? classroomId,

            HttpContext httpContext,

            IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(new GetQuizzesQuery(courseId, classroomId, userId, role), cancellationToken));

        }).RequireAuthorization();

        app.MapGet("/api/quizzes/{quizId:guid}", async (

            Guid quizId,

            HttpContext httpContext,

            IQueryHandler<GetQuizByIdQuery, QuizDto?> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var quiz = await handler.Handle(new GetQuizByIdQuery(quizId, userId, role), cancellationToken);

            return quiz is null ? Results.NotFound() : Results.Ok(quiz);

        }).RequireAuthorization();

        app.MapPost("/api/quizzes", async (

            CreateQuizRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateQuizCommand, QuizDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateQuizCommand(

                        userId,

                        request.CourseId,

                        request.ClassroomId,

                        request.Title,

                        request.Description,

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

        app.MapPost("/api/quizzes/submit", async (

            SubmitQuizRequest request,

            HttpContext httpContext,

            ICommandHandler<SubmitQuizCommand, SubmitQuizResponse> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                var result = await handler.Handle(

                    new SubmitQuizCommand(userId, request.QuizId, request.Answers),

                    cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/teacher/quizzes", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            int? grade,
            Guid? courseId,
            HttpContext httpContext,
            IQueryHandler<GetTeacherQuizzesQuery, IReadOnlyList<TeacherQuizListDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetTeacherQuizzesQuery(userId, fromDate, toDate, grade, courseId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/teacher/quizzes/{quizId:guid}", async (
            Guid quizId,
            HttpContext httpContext,
            IQueryHandler<GetTeacherQuizByIdQuery, TeacherQuizDetailDto?> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                var quiz = await handler.Handle(new GetTeacherQuizByIdQuery(userId, quizId), cancellationToken);
                return quiz is null ? Results.NotFound() : Results.Ok(quiz);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPut("/api/teacher/quizzes/{quizId:guid}", async (
            Guid quizId,
            UpdateQuizRequest request,
            HttpContext httpContext,
            ICommandHandler<UpdateQuizCommand, QuizDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new UpdateQuizCommand(
                        userId,
                        quizId,
                        request.CourseId,
                        request.ClassroomId,
                        request.Title,
                        request.Description,
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

        app.MapPost("/api/teacher/quizzes/{quizId:guid}/publish", async (
            Guid quizId,
            HttpContext httpContext,
            ICommandHandler<PublishQuizCommand, QuizDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(new PublishQuizCommand(userId, quizId), cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/teacher/quizzes/{quizId:guid}", async (
            Guid quizId,
            HttpContext httpContext,
            ICommandHandler<DeleteQuizCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                await handler.Handle(new DeleteQuizCommand(userId, quizId), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/teacher/quizzes/{quizId:guid}/attempts", async (
            Guid quizId,
            HttpContext httpContext,
            IQueryHandler<GetQuizAttemptsQuery, IReadOnlyList<QuizAttemptReviewDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetQuizAttemptsQuery(userId, quizId),
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
