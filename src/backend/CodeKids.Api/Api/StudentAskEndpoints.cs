using System.Security.Claims;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.StudentAsk;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Api;

public static class StudentAskEndpoints
{
    private static readonly AuthorizeAttribute TeacherOrAdmin =
        new() { Roles = "Teacher,SuperAdmin" };

    public static IEndpointRouteBuilder MapStudentAskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/student-ask", async (
            SetStudentAskEnabledRequest request,
            HttpContext httpContext,
            IAppDbContext dbContext,
            ICommandHandler<SetStudentAskEnabledCommand, StudentAskSettingsDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await EnsureCanManageScopeAsync(httpContext, dbContext, request.Scope, request.Id, cancellationToken);
                return Results.Ok(await handler.Handle(
                    new SetStudentAskEnabledCommand(request.Scope, request.Id, request.Enabled),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(TeacherOrAdmin);

        app.MapPost("/api/student-ask", async (
            AskStudentQuestionRequest request,
            HttpContext httpContext,
            ICommandHandler<AskStudentQuestionCommand, StudentAskAnswerDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var studentId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new AskStudentQuestionCommand(
                        studentId,
                        request.Question,
                        request.CourseId,
                        request.UnitId,
                        request.LessonId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

        return app;
    }

    private static async Task EnsureCanManageScopeAsync(
        HttpContext httpContext,
        IAppDbContext dbContext,
        string? scope,
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;

        var normalized = (scope ?? string.Empty).Trim().ToLowerInvariant();
        Guid courseId;
        if (normalized == "course")
        {
            courseId = id;
        }
        else if (normalized == "unit")
        {
            var unit = await CourseOutlineResolver.FindUnitAsync(dbContext, id, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            courseId = unit.Course.Id;
        }
        else if (normalized == "lesson")
        {
            var lesson = await CourseOutlineResolver.FindLessonAsync(dbContext, id, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            courseId = lesson.Course.Id;
        }
        else
        {
            throw new InvalidOperationException("Ask scope must be course, unit, or lesson.");
        }

        await CourseTreeAccess.EnsureCanManageCourseAsync(dbContext, userId, role, courseId, cancellationToken);
    }
}
