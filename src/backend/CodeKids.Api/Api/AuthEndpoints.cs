using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AuthEndpoints

{

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapPost("/api/auth/register", async (

            RegisterRequest request,

            ICommandHandler<RegisterCommand, AuthResponse> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var result = await handler.Handle(

                    new RegisterCommand(request.Email, request.DisplayName, request.Password, request.Role, request.ParentId),

                    cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        });

        app.MapPost("/api/auth/login", async (

            LoginRequest request,

            ICommandHandler<LoginCommand, AuthResponse> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var result = await handler.Handle(new LoginCommand(request.Email, request.Password), cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        });

        app.MapPost("/api/auth/forgot-password", async (

            ForgotPasswordRequest request,

            ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var result = await handler.Handle(new ForgotPasswordCommand(request.Email), cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        });

        app.MapPost("/api/auth/reset-password", async (

            ResetPasswordRequest request,

            ICommandHandler<ResetPasswordCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);

                return Results.Ok(new { accepted = true });

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        });

        app.MapPut("/api/auth/account", async (
            HttpContext httpContext,
            UpdateOwnAccountRequest request,
            ICommandHandler<UpdateOwnAccountCommand, AuthUserDto> handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var userId = CurrentUser.GetUserId(httpContext.User);
                return Results.Ok(await handler.Handle(
                    new UpdateOwnAccountCommand(userId, request.Email, request.MobilePhone, request.Password),
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
