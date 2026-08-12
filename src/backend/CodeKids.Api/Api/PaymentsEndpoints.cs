using CodeKids.Application.Features.Payments;

using CodeKids.Domain.Abstractions;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class PaymentsEndpoints

{

    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/payments", async (

            Guid? parentId,

            Guid? studentId,

            DateOnly? fromDate,

            DateOnly? toDate,

            int? year,

            int? month,

            IQueryHandler<ListTuitionPaymentsQuery, IReadOnlyList<TuitionPaymentDto>> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new ListTuitionPaymentsQuery(parentId, studentId, fromDate, toDate, year, month),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/admin/payments", async (

            CreateTuitionPaymentRequest request,

            ICommandHandler<CreateTuitionPaymentCommand, TuitionPaymentDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateTuitionPaymentCommand(

                        request.ParentId,

                        request.StudentId,

                        request.Year,

                        request.Month,

                        request.Amount,

                        request.PaymentDate,

                        request.Notes),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/admin/payments/{paymentId:guid}", async (

            Guid paymentId,

            ICommandHandler<DeleteTuitionPaymentCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteTuitionPaymentCommand(paymentId), cancellationToken);

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
