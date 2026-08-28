using CodeKids.Application.Features.Lessons;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Api;

public static class LessonsEndpoints
{
    public static IEndpointRouteBuilder MapLessonsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lessons", async (
            Guid? courseId,
            IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new GetLessonsQuery(courseId), cancellationToken));
        }).RequireAuthorization();

        app.MapGet("/api/lessons/{lessonId:guid}", async (
            Guid lessonId,
            IQueryHandler<GetLessonByIdQuery, LessonDto?> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var lesson = await handler.Handle(new GetLessonByIdQuery(lessonId), cancellationToken);
                return lesson is null ? Results.NotFound() : Results.Ok(lesson);
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization();

        return app;
    }
}
