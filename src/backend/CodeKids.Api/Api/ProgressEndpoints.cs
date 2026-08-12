using CodeKids.Application.Features.Progress;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ProgressEndpoints

{

    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapPost("/api/progress/complete-step", async (

            CompleteStepRequest request,

            HttpContext httpContext,

            ICommandHandler<CompleteStepCommand, CompleteStepResponse> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                var result = await handler.Handle(

                    new CompleteStepCommand(userId, request.LessonId, request.StepId, request.SubmittedAnswer),

                    cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/progress/me", async (

            HttpContext httpContext,

            IQueryHandler<GetStudentSummaryQuery, StudentSummaryDto> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            return Results.Ok(await handler.Handle(new GetStudentSummaryQuery(userId), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        return app;

    }

}
