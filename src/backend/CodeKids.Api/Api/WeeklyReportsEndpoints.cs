using CodeKids.Application.Abstractions;

using CodeKids.Application.Features.Media;

using CodeKids.Application.Features.WeeklyReports;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class WeeklyReportsEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyReportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weekly-reports/grid", async (
            HttpContext httpContext,
            DateOnly weekStart,
            int? grade,
            IQueryHandler<GetWeeklyReportGridQuery, IReadOnlyList<StudentWeeklyReportGridRowDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetWeeklyReportGridQuery(teacherId, weekStart, grade),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/weekly-reports/top-students", async (
            DateOnly? weekStart,
            IQueryHandler<ListTopWeeklyStudentsQuery, IReadOnlyList<TopWeeklyStudentDto>> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(
                new ListTopWeeklyStudentsQuery(weekStart),
                cancellationToken));
        }).AllowAnonymous();

        app.MapGet("/api/weekly-reports/top-students/{studentId:guid}/photo", async (
            Guid studentId,
            DateOnly? week,
            IAppDbContext dbContext,
            IFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var weekStart = week ?? ListTopWeeklyStudentsQueryHandler.StartOfWeek(
                DateOnly.FromDateTime(DateTime.UtcNow));
            if (!await ListTopWeeklyStudentsQueryHandler.QualifiesForBoardAsync(
                    dbContext, studentId, weekStart, cancellationToken))
            {
                return Results.NotFound();
            }

            var photo = await dbContext.Users.AsNoTracking()
                .Where(x => x.Id == studentId)
                .Select(x => new { x.ProfilePhotoStorageKey, x.ProfilePhotoContentType })
                .FirstOrDefaultAsync(cancellationToken);
            if (photo is null || string.IsNullOrWhiteSpace(photo.ProfilePhotoStorageKey))
            {
                return Results.NotFound();
            }

            var stream = await fileStorage.OpenReadAsync(photo.ProfilePhotoStorageKey, cancellationToken);
            var contentType = MediaFileTypes.ResolveContentType(
                photo.ProfilePhotoContentType,
                photo.ProfilePhotoStorageKey);
            return Results.File(stream, contentType);
        }).AllowAnonymous();

        app.MapGet("/api/weekly-reports", async (
            HttpContext httpContext,
            int? grade,
            DateOnly? fromDate,
            DateOnly? toDate,
            IQueryHandler<ListStudentWeeklyReportsQuery, IReadOnlyList<StudentWeeklyReportDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new ListStudentWeeklyReportsQuery(teacherId, grade, fromDate, toDate),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapGet("/api/admin/weekly-reports", async (
            Guid? teacherId,
            int? grade,
            DateOnly? fromDate,
            DateOnly? toDate,
            IQueryHandler<ListStudentWeeklyReportsQuery, IReadOnlyList<StudentWeeklyReportDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new ListStudentWeeklyReportsQuery(teacherId, grade, fromDate, toDate),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/weekly-reports", async (
            HttpContext httpContext,
            SaveWeeklyReportsRequest request,
            ICommandHandler<SaveWeeklyReportsCommand, IReadOnlyList<StudentWeeklyReportGridRowDto>> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new SaveWeeklyReportsCommand(teacherId, request.WeekStartDate, request.Entries),
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
