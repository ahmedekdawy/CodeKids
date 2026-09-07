using CodeKids.Application.Features.Analytics;

using CodeKids.Application.Features.Classrooms;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace CodeKids.Api;

public static class ClassroomsEndpoints

{

    public static IEndpointRouteBuilder MapClassroomsEndpoints(this IEndpointRouteBuilder app)

    {

        app.MapGet("/api/classrooms", async (

            HttpContext httpContext,

            IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(new GetClassroomsQuery(userId, role), cancellationToken));

        }).RequireAuthorization();

        app.MapPost("/api/classrooms", async (

            CreateClassroomRequest request,

            ICommandHandler<CreateClassroomCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new CreateClassroomCommand(

                        request.Name,

                        request.Description,

                        request.Grade,

                        request.Courses,

                        request.WhatsAppGroupInviteUrl,

                        request.ZoomLinks,

                        request.WhatsAppNotifyPhones),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/classrooms/{classroomId:guid}", async (

            Guid classroomId,

            UpdateClassroomRequest request,

            ICommandHandler<UpdateClassroomCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateClassroomCommand(

                        classroomId,

                        request.Name,

                        request.Description,

                        request.Grade,

                        request.Courses,

                        request.WhatsAppGroupInviteUrl,

                        request.ZoomLinks,

                        request.WhatsAppNotifyPhones),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapDelete("/api/classrooms/{classroomId:guid}", async (

            Guid classroomId,

            ICommandHandler<DeleteClassroomCommand, bool> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                await handler.Handle(new DeleteClassroomCommand(classroomId), cancellationToken);

                return Results.NoContent();

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/classrooms/{classroomId:guid}/assignments", async (

            Guid classroomId,

            AssignClassroomRequest request,

            ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateClassroomAssignmentsCommand(classroomId, request.Courses),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/classrooms/{classroomId:guid}/students", async (

            Guid classroomId,

            AddClassroomStudentRequest request,

            ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new AddStudentToClassroomCommand(classroomId, request.StudentId, request.CourseIds),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPost("/api/classrooms/{classroomId:guid}/whatsapp/send", async (

            Guid classroomId,

            SendClassroomWhatsAppRequest request,

            HttpContext httpContext,

            ICommandHandler<SendClassroomWhatsAppCommand, SendClassroomWhatsAppResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                return Results.Ok(await handler.Handle(

                    new SendClassroomWhatsAppCommand(

                        userId,

                        classroomId,

                        request.Message,

                        request.StudentIds,

                        request.IncludeGroupInviteLink,
                        request.SendToGroup,
                        request.GroupId),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

        app.MapDelete("/api/classrooms/{classroomId:guid}/students/{studentId:guid}", async (

            Guid classroomId,

            Guid studentId,

            ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new RemoveStudentFromClassroomCommand(classroomId, studentId),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapPut("/api/classrooms/{classroomId:guid}/whatsapp", async (

            Guid classroomId,

            UpdateClassroomWhatsAppRequest request,

            ICommandHandler<UpdateClassroomWhatsAppCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                return Results.Ok(await handler.Handle(

                    new UpdateClassroomWhatsAppCommand(

                        classroomId,

                        request.WhatsAppGroupInviteUrl,

                        request.WhatsAppNotifyPhones,

                        request.DailyWhatsAppReportsEnabled),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin,Teacher" });

        app.MapPut("/api/classrooms/{classroomId:guid}/zoom", async (

            Guid classroomId,

            UpdateClassroomZoomRequest request,

            HttpContext httpContext,

            ICommandHandler<UpdateClassroomZoomCommand, ClassroomDto> handler,

            CancellationToken cancellationToken) =>

        {

            try

            {

                var userId = CurrentUser.GetUserId(httpContext.User);

                var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

                return Results.Ok(await handler.Handle(

                    new UpdateClassroomZoomCommand(

                        classroomId,

                        userId,

                        role,

                        request.ZoomLinks),

                    cancellationToken));

            }

            catch (Exception ex)

            {

                return ApiResults.ProblemFromException(ex);

            }

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin,Teacher" });

        app.MapGet("/api/classrooms/enrollments", async (

            HttpContext httpContext,

            Guid? classroomId,

            Guid? courseId,

            string? studentSearch,

            string? sortKey,

            string? sortDir,

            int? page,

            int? pageSize,

            IQueryHandler<ListClassroomEnrollmentsQuery, PagedClassroomEnrollmentsResultDto> handler,

            CancellationToken cancellationToken) =>

        {

            var userId = CurrentUser.GetUserId(httpContext.User);

            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Results.Ok(await handler.Handle(

                new ListClassroomEnrollmentsQuery(

                    userId,

                    role,

                    classroomId,

                    courseId,

                    studentSearch,

                    sortKey ?? "classroomName",

                    sortDir ?? "asc",

                    page ?? 1,

                    pageSize ?? 10),

                cancellationToken));

        }).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin,Teacher" });

        return app;

    }

}
