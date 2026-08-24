using CodeKids.Application.Features.Subjects;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class SubjectsEndpoints
{
    public static IEndpointRouteBuilder MapSubjectsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/subjects", async (
            int? stageId,
            IQueryHandler<ListSubjectsQuery, IReadOnlyList<SubjectDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(new ListSubjectsQuery(stageId), cancellationToken));
        }).RequireAuthorization();

        return app;
    }
}
