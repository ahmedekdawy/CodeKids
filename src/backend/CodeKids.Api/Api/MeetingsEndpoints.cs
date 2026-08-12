using CodeKids.Application.Features.Meetings;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class MeetingsEndpoints

{

    public static IEndpointRouteBuilder MapMeetingsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/meetings", async (

            HttpContext httpContext,

            IQueryHandler<GetMeetingsQuery, IReadOnlyList<LiveSessionDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(new GetMeetingsQuery(userId, role), cancellationToken));

        }).RequireAuthorization();

        app.MapPost("/api/meetings", async (

            CreateMeetingRequest request,

            HttpContext httpContext,

            ICommandHandler<CreateMeetingCommand, LiveSessionDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                var result = await handler.Handle(

                    new CreateMeetingCommand(

                        userId,

                        request.Title,

                        request.Description,

                        request.StartsAtUtc,

                        request.DurationMinutes,

                        request.ClassroomId,

                        request.CourseId,

                        request.NotifyWhatsApp),

                    cancellationToken);

                return Results.Ok(result);

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        return app;

    }

}
