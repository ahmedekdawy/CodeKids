using CodeKids.Application.Features.Analytics;

using CodeKids.Domain.Abstractions;

using Microsoft.AspNetCore.Authorization;

namespace CodeKids.Api;

public static class ReportsEndpoints

{

    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapPost("/api/reports/whatsapp/daily", async (

            bool? force,

            ICommandHandler<RunDailyWhatsAppReportsCommand, DailyWhatsAppReportsResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new RunDailyWhatsAppReportsCommand(Force: force == true),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

        return app;

    }

}
