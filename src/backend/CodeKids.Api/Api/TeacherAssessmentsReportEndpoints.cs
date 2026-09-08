using CodeKids.Application.Features.Reports;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class TeacherAssessmentsReportEndpoints
{
    public static IEndpointRouteBuilder MapTeacherAssessmentsReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/teacher-assessments", async (
            Guid? teacherId,
            Guid? classroomId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? status,
            string? kind,
            int? page,
            int? pageSize,
            IQueryHandler<GetTeacherAssessmentsReportQuery, PagedTeacherAssessmentsResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new GetTeacherAssessmentsReportQuery(
                        teacherId,
                        classroomId,
                        fromDate,
                        toDate,
                        status,
                        kind,
                        page ?? 1,
                        pageSize ?? 10),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        return app;
    }
}
