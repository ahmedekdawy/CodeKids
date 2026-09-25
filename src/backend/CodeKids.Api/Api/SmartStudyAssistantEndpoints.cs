using CodeKids.Application.Features.SmartStudyAssistant;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class SmartStudyAssistantEndpoints
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;
    private const int MaxFiles = 10;

    public static IEndpointRouteBuilder MapSmartStudyAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        // Accepts multipart/form-data so the teacher can attach multiple PDFs/images;
        // everything else arrives as form fields.
        app.MapPost("/api/smart-study-assistant/generate", async (
            HttpContext httpContext,
            ICommandHandler<GenerateSmartStudyAssistantCommand, SmartStudyAssistantResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                var form = await httpContext.Request.ReadFormAsync(cancellationToken);

                string? action = form["action"];
                if (!Guid.TryParse(form["courseId"], out var courseId) || courseId == Guid.Empty)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["courseId"] = ["A valid courseId is required."]
                    });
                }

                Guid.TryParse(form["unitId"], out var unitId);
                Guid.TryParse(form["lessonId"], out var lessonId);
                var language = form["language"].ToString();

                var attachments = new List<SmartStudyAttachmentFile>();
                foreach (var file in form.Files.Take(MaxFiles))
                {
                    if (file.Length == 0)
                    {
                        continue;
                    }

                    if (file.Length > MaxUploadBytes)
                    {
                        return Results.Problem(
                            title: "File too large.",
                            detail: $"'{file.FileName}' exceeds the 25 MB limit.",
                            statusCode: StatusCodes.Status413PayloadTooLarge);
                    }

                    await using var stream = file.OpenReadStream();
                    using var buffer = new MemoryStream();
                    await stream.CopyToAsync(buffer, cancellationToken);
                    attachments.Add(new SmartStudyAttachmentFile(
                        file.FileName,
                        string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
                        buffer.ToArray()));
                }

                return Results.Ok(await handler.Handle(
                    new GenerateSmartStudyAssistantCommand(
                        teacherId,
                        action ?? string.Empty,
                        courseId,
                        unitId == Guid.Empty ? null : unitId,
                        lessonId == Guid.Empty ? null : lessonId,
                        language,
                        attachments),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        })
        .DisableAntiforgery()
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/smart-study-assistant/apply", async (
            ApplySmartStudyAssistantRequest request,
            HttpContext httpContext,
            ICommandHandler<ApplySmartStudyAssistantCommand, ApplySmartStudyAssistantResultDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var teacherId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new ApplySmartStudyAssistantCommand(
                        teacherId,
                        request.CourseId,
                        request.Target,
                        request.Title,
                        request.Description,
                        request.UnitId,
                        request.LessonId,
                        request.Mode,
                        request.Questions,
                        request.Units),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        return app;
    }
}
