using CodeKids.Application.Features.Admin;

using CodeKids.Application.Features.Auth;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AdminUsersEndpoints

{

    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/users", async (

            string? role,

            IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>> handler,

            CancellationToken cancellationToken) =>

        {

            return Results.Ok(await handler.Handle(new ListManagedUsersQuery(role), cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/users", async (

            CreateManagedUserRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateManagedUserCommand, ManagedUserDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new CreateManagedUserCommand(

                        adminId,

                        request.Email,

                        request.DisplayName,

                        request.Password,

                        request.Role,

                        request.ParentId,

                        request.Grade,

                        request.SchoolType,

                        request.MobilePhone,

                        request.WorkShift,

                        request.Stages,

                        request.ContractType,

                        request.PrimaryAmount,

                        request.PrepAmount,

                        request.SecondaryAmount,

                        request.MonthlySalary,

                        request.CourseRates),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/admin/users/{userId:guid}", async (

            Guid userId,

            UpdateManagedUserRequest request,

            HttpContext httpContext,

            ICommandHandler<UpdateManagedUserCommand, ManagedUserDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new UpdateManagedUserCommand(

                        adminId,

                        userId,

                        request.Email,

                        request.DisplayName,

                        request.Role,

                        request.ParentId,

                        request.Password,

                        request.Grade,

                        request.SchoolType,

                        request.MobilePhone,

                        request.WorkShift,

                        request.Stages,

                        request.ContractType,

                        request.PrimaryAmount,

                        request.PrepAmount,

                        request.SecondaryAmount,

                        request.MonthlySalary,

                        request.CourseRates),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/users/{userId:guid}/active", async (

            Guid userId,

            SetManagedUserActiveRequest request,

            HttpContext httpContext,

            ICommandHandler<SetManagedUserActiveCommand, ManagedUserDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new SetManagedUserActiveCommand(adminId, userId, request.IsActive),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/users/{userId:guid}", async (

            Guid userId,

            HttpContext httpContext,

            ICommandHandler<DeleteManagedUserCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                await handler.Handle(new DeleteManagedUserCommand(adminId, userId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/users/{userId:guid}/impersonate", async (

            Guid userId,

            HttpContext httpContext,

            ICommandHandler<ImpersonateUserCommand, AuthResponse> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var adminId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(new ImpersonateUserCommand(adminId, userId), cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        return app;

    }

}
