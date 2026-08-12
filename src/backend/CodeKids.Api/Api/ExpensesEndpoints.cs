using CodeKids.Application.Features.Expenses;

using CodeKids.Domain.Abstractions;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ExpensesEndpoints

{

    public static IEndpointRouteBuilder MapExpensesEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/other-expenses", async (

            DateOnly? fromDate,

            DateOnly? toDate,

            string? name,

            IQueryHandler<ListOtherExpensesQuery, IReadOnlyList<OtherExpenseDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new ListOtherExpensesQuery(fromDate, toDate, name),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/other-expenses", async (

            CreateOtherExpenseRequest request,

            ICommandHandler<CreateOtherExpenseCommand, OtherExpenseDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateOtherExpenseCommand(

                        request.Name,

                        request.Amount,

                        request.ExpenseDate,

                        request.Notes),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/other-expenses/{expenseId:guid}", async (

            Guid expenseId,

            ICommandHandler<DeleteOtherExpenseCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteOtherExpenseCommand(expenseId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        return app;

    }

}
