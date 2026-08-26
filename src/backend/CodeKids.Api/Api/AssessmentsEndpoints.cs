using CodeKids.Application.Features.Assessments;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AssessmentsEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assessments/generate", async (
            GenerateAssessmentDraftRequest request,
            HttpContext httpContext,
            ICommandHandler<GenerateAssessmentDraftCommand, GeneratedAssessmentDraftDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GenerateAssessmentDraftCommand(
                        teacherId,
                        request.Kind,
                        request.CourseId,
                        request.ClassroomId,
                        request.UnitIds,
                        request.LessonIds,
                        request.QuestionCount,
                        request.QuestionType,
                        request.Language),
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
