using CodeKids.Application.Features.Quizzes;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class QuizzesEndpoints

{

    public static IEndpointRouteBuilder MapQuizzesEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/quizzes", async (

            Guid? courseId,

            Guid? classroomId,

            IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(new GetQuizzesQuery(courseId, classroomId), cancellationToken));

        }).RequireAuthorization();

        app.MapGet("/api/quizzes/{quizId:guid}", async (

            Guid quizId,

            IQueryHandler<GetQuizByIdQuery, QuizDto?> handler,

            CancellationToken cancellationToken) =>

        {

            var quiz = await handler.Handle(new GetQuizByIdQuery(quizId), cancellationToken);

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

        return app;

    }

}
