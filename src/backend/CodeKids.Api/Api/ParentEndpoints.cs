using CodeKids.Application.Features.Auth;
using CodeKids.Application.Features.Dashboard;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ParentEndpoints
{
    public static IEndpointRouteBuilder MapParentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/parent/children/{childId:guid}/impersonate", async (
            Guid childId,
            HttpContext httpContext,
            ICommandHandler<ImpersonateUserCommand, AuthResponse> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var parentId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new ImpersonateUserCommand(parentId, childId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        app.MapGet("/api/parent/children/{childId:guid}", async (
            Guid childId,
            HttpContext httpContext,
            IQueryHandler<GetParentChildOverviewQuery, ParentChildOverviewDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new GetParentChildOverviewQuery(userId, childId),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        app.MapPut("/api/parent/accounts/{userId:guid}", async (
            Guid userId,
            HttpContext httpContext,
            UpdateParentManagedAccountRequest request,
            ICommandHandler<UpdateParentManagedAccountCommand, ParentManagedAccountDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var parentId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new UpdateParentManagedAccountCommand(
                        parentId,
                        userId,
                        request.Email,
                        request.MobilePhone,
                        request.Password),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResults.ProblemFromException(ex);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

        return app;
    }
}
