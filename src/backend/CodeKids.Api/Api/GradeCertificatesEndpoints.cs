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

        // Approval: only admins approve; students and parents only ever see approved certificates.
        app.MapPost("/api/admin/grade-certificates/{id:guid}/approve", async (
            Guid id,
            HttpContext httpContext,
            ICommandHandler<ApproveGradeCertificateCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var changed = await handler.Handle(
                    new ApproveGradeCertificateCommand(id, CurrentUser.GetUserId(httpContext.User)),
                    cancellationToken);
                return Results.Ok(new { changed });
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/grade-certificates/approve-all", async (
            HttpContext httpContext,
            ICommandHandler<ApproveAllGradeCertificatesCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var count = await handler.Handle(
                    new ApproveAllGradeCertificatesCommand(CurrentUser.GetUserId(httpContext.User)),
                    cancellationToken);
                return Results.Ok(new { approvedCount = count });
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/grade-certificates/{id:guid}/revoke-approval", async (
            Guid id,
            ICommandHandler<RevokeGradeCertificateApprovalCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var changed = await handler.Handle(
                    new RevokeGradeCertificateApprovalCommand(id),
                    cancellationToken);
                return Results.Ok(new { changed });
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        // Approved certificates for the student and their parent. A student may only read their own.
        app.MapGet("/api/students/{studentId:guid}/grade-certificates", async (
            Guid studentId,
            HttpContext httpContext,
            IQueryHandler<ListStudentGradeCertificatesQuery, IReadOnlyList<StudentGradeCertificateDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (CurrentUser.GetUserId(httpContext.User) != studentId)
                {
                    throw new UnauthorizedAccessException("You can only view your own certificates.");
                }

                return Results.Ok(await handler.Handle(
                    new ListStudentGradeCertificatesQuery(studentId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        app.MapGet("/api/parent/children/{childId:guid}/grade-certificates", async (
            Guid childId,
            HttpContext httpContext,
            IQueryHandler<ListChildGradeCertificatesQuery, IReadOnlyList<StudentGradeCertificateDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new ListChildGradeCertificatesQuery(CurrentUser.GetUserId(httpContext.User), childId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        return app;
    }

    private static bool IsAdmin(HttpContext httpContext) => httpContext.User.IsInRole("SuperAdmin");
}
