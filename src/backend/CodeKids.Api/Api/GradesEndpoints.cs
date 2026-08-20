using CodeKids.Application.Features.Grades;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class GradesEndpoints
{
    public static IEndpointRouteBuilder MapGradesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stages", async (
            IQueryHandler<ListStagesQuery, IReadOnlyList<StageDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListStagesQuery(), cancellationToken));
        }).RequireAuthorization();

        app.MapGet("/api/grades", async (
            int? stageId,
            IQueryHandler<ListGradesQuery, IReadOnlyList<GradeDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListGradesQuery(stageId), cancellationToken));
        }).RequireAuthorization();

        return app;
    }
}
