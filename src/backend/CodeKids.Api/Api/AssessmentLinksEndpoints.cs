using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.AssessmentLinks;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodeKids.Api;

public static class AssessmentLinksEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentLinksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assessment-links", async (
            string kind,
            Guid resourceId,
            HttpContext httpContext,
            IQueryHandler<GetAssessmentStudentLinksQuery, AssessmentStudentLinksResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var parsedKind = ParseKind(kind);
                var userId = CurrentUser.GetUserId(httpContext.User);
                var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                return Results.Ok(await handler.Handle(
                    new GetAssessmentStudentLinksQuery(userId, role, parsedKind, resourceId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        app.MapPost("/api/auth/assessment-link/redeem", async (
            RedeemAssessmentLinkRequest request,
            ICommandHandler<RedeemAssessmentLinkCommand, RedeemAssessmentLinkResult> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await handler.Handle(
                    new RedeemAssessmentLinkCommand(request.Key ?? string.Empty),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).AllowAnonymous();

        return app;
    }

    private static AssessmentLinkKind ParseKind(string kind)
    {
        if (Enum.TryParse<AssessmentLinkKind>(kind, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException("Kind must be exam, quiz, or assignment.");
    }
}

public sealed record RedeemAssessmentLinkRequest(string? Key);
