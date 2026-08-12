using CodeKids.Application.Features.QuestionBank;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class QuestionBankEndpoints

{

    public static IEndpointRouteBuilder MapQuestionBankEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/question-bank", async (

            Guid? courseId,

            Guid? lessonId,

            HttpContext httpContext,

            IQueryHandler<ListBankQuestionsQuery, IReadOnlyList<BankQuestionDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            return Results.Ok(await handler.Handle(new ListBankQuestionsQuery(userId, courseId, lessonId), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPost("/api/question-bank", async (

            CreateBankQuestionRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateBankQuestionCommand, BankQuestionDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateBankQuestionCommand(

                        userId,

                        request.CourseId,

                        request.LessonId,

                        request.QuestionType,

                        request.Prompt,

                        request.PassageText,

                        request.OptionA,

                        request.OptionB,

                        request.OptionC,

                        request.OptionD,

                        request.Options,

                        request.CorrectAnswer,

                        request.Points,

                        request.SortOrder,

                        request.Children),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapPut("/api/question-bank/{questionId:guid}", async (

            Guid questionId,

            UpdateBankQuestionRequest request,

            HttpContext httpContext,

            ICommandHandler<UpdateBankQuestionCommand, BankQuestionDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new UpdateBankQuestionCommand(

                        userId,

                        questionId,

                        request.LessonId,

                        request.Prompt,

                        request.PassageText,

                        request.OptionA,

                        request.OptionB,

                        request.OptionC,

                        request.OptionD,

                        request.Options,

                        request.CorrectAnswer,

                        request.Points,

                        request.SortOrder),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/question-bank/{questionId:guid}", async (

            Guid questionId,

            HttpContext httpContext,

            ICommandHandler<DeleteBankQuestionCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                await handler.Handle(new DeleteBankQuestionCommand(userId, questionId), cancellationToken);

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
