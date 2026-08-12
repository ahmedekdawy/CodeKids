using CodeKids.Application.Features.Reports;

using CodeKids.Domain.Abstractions;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class AccountReportEndpoints

{

    public static IEndpointRouteBuilder MapAccountReportEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/admin/account-report", async (

            DateOnly fromDate,

            DateOnly toDate,

            IQueryHandler<GetAccountReportQuery, AccountReportDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new GetAccountReportQuery(fromDate, toDate),

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
