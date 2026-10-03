using CodeKids.Application.Features.GradeCertificates;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class GradeCertificatesEndpoints
{
    public static IEndpointRouteBuilder MapGradeCertificatesEndpoints(this IEndpointRouteBuilder app)
    {
        // Admins see every certificate; teachers only those with a subject they teach.
        app.MapGet("/api/grade-certificates", async (
            HttpContext httpContext,
            IQueryHandler<ListGradeCertificatesQuery, IReadOnlyList<GradeCertificateListItemDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new ListGradeCertificatesQuery(CurrentUser.GetUserId(httpContext.User), IsAdmin(httpContext)),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapGet("/api/grade-certificates/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IQueryHandler<GetGradeCertificateSheetQuery, GradeCertificateSheetDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new GetGradeCertificateSheetQuery(id, CurrentUser.GetUserId(httpContext.User), IsAdmin(httpContext)),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPut("/api/grade-certificates/{id:guid}/marks", async (
            Guid id,
            SaveGradeCertificateMarksRequest request,
            HttpContext httpContext,
            ICommandHandler<SaveGradeCertificateMarksCommand, GradeCertificateSheetDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new SaveGradeCertificateMarksCommand(
                        id,
                        CurrentUser.GetUserId(httpContext.User),
                        IsAdmin(httpContext),
                        request.Entries),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/admin/grade-certificates", async (
            SaveGradeCertificateRequest request,
            HttpContext httpContext,
            ICommandHandler<SaveGradeCertificateCommand, GradeCertificateSheetDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new SaveGradeCertificateCommand(
                        null,
                        CurrentUser.GetUserId(httpContext.User),
                        request.ClassroomId,
                        request.Title,
                        request.Subjects),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/grade-certificates/{id:guid}", async (
            Guid id,
            SaveGradeCertificateRequest request,
            HttpContext httpContext,
            ICommandHandler<SaveGradeCertificateCommand, GradeCertificateSheetDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new SaveGradeCertificateCommand(
                        id,
                        CurrentUser.GetUserId(httpContext.User),
                        request.ClassroomId,
                        request.Title,
                        request.Subjects),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/grade-certificates/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteGradeCertificateCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.Handle(new DeleteGradeCertificateCommand(id), cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        return app;
    }

    private static bool IsAdmin(HttpContext httpContext) => httpContext.User.IsInRole("SuperAdmin");
}
