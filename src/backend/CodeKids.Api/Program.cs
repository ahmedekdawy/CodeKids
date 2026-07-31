using System.Security.Claims;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using CodeKids.Application.Features.Admin;
using CodeKids.Application.Features.Assignments;
using CodeKids.Application.Features.Auth;
using CodeKids.Application.Features.Avatars;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.Dashboard;
using CodeKids.Application.Features.Lessons;
using CodeKids.Application.Features.Meetings;
using CodeKids.Application.Features.Progress;
using CodeKids.Application.Features.Quizzes;
using CodeKids.Application.Features.ZoomConnect;
using CodeKids.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeKids API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddZoomIntegration(builder.Configuration);
builder.Services.AddWhatsAppIntegration(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<ICommandHandler<RegisterCommand, AuthResponse>, RegisterCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>, GetCoursesQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, CourseSummaryDto>, CreateCourseCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCourseCommand, CourseSummaryDto>, UpdateCourseCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCourseCommand, bool>, DeleteCourseCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>, GetLessonsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetLessonByIdQuery, LessonDto?>, GetLessonByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteStepCommand, CompleteStepResponse>, CompleteStepCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudentSummaryQuery, StudentSummaryDto>, GetStudentSummaryQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>>, GetQuizzesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetQuizByIdQuery, QuizDto?>, GetQuizByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SubmitQuizCommand, SubmitQuizResponse>, SubmitQuizCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateQuizCommand, QuizDto>, CreateQuizCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>>, GetBadgesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetAvatarsQuery, IReadOnlyList<AvatarDto>>, GetAvatarsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SelectAvatarCommand, AvatarDto>, SelectAvatarCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>, GetParentDashboardQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetTeacherDashboardQuery, TeacherDashboardDto>, GetTeacherDashboardQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateMeetingCommand, LiveSessionDto>, CreateMeetingCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetMeetingsQuery, IReadOnlyList<LiveSessionDto>>, GetMeetingsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetZoomConnectUrlQuery, ZoomConnectUrlDto>, GetZoomConnectUrlQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteZoomConnectCommand, ZoomConnectResultDto>, CompleteZoomConnectCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetZoomStatusQuery, ZoomConnectionStatus>, GetZoomStatusQueryHandler>();
builder.Services.AddScoped<ICommandHandler<DisconnectZoomCommand, bool>, DisconnectZoomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateManagedUserCommand, ManagedUserDto>, CreateManagedUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateManagedUserCommand, ManagedUserDto>, UpdateManagedUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteManagedUserCommand, bool>, DeleteManagedUserCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>, ListManagedUsersQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateClassroomCommand, ClassroomDto>, CreateClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomCommand, ClassroomDto>, UpdateClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteClassroomCommand, bool>, DeleteClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>, UpdateClassroomAssignmentsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<AddStudentToClassroomCommand, ClassroomDto>, AddStudentToClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto>, RemoveStudentFromClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomWhatsAppCommand, ClassroomDto>, UpdateClassroomWhatsAppCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>>, GetClassroomsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassroomByIdQuery, ClassroomDto?>, GetClassroomByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateAssignmentCommand, AssignmentDto>, CreateAssignmentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>>, GetAssignmentsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?>, GetAssignmentByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto>, SubmitAssignmentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>>, GetAssignmentSubmissionsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto>, GradeSubmissionCommandHandler>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await SchemaBootstrap.EnsureAsync(dbContext);
    await DataSeeder.SeedAsync(dbContext, passwordHasher);
}

static IResult ProblemFromException(Exception ex) =>
    Results.BadRequest(new { message = ex.Message });

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
        return ProblemFromException(ex);
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
        return ProblemFromException(ex);
    }
});

app.MapGet("/api/courses", async (
    IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetCoursesQuery(), cancellationToken));
}).RequireAuthorization();

app.MapPost("/api/admin/courses", async (
    CreateCourseRequest request,
    ICommandHandler<CreateCourseCommand, CourseSummaryDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new CreateCourseCommand(request.Title, request.Theme, request.Description, request.AgeMin, request.AgeMax, request.SortOrder),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapPut("/api/admin/courses/{courseId:guid}", async (
    Guid courseId,
    UpdateCourseRequest request,
    ICommandHandler<UpdateCourseCommand, CourseSummaryDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new UpdateCourseCommand(courseId, request.Title, request.Theme, request.Description, request.AgeMin, request.AgeMax, request.SortOrder),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapDelete("/api/admin/courses/{courseId:guid}", async (
    Guid courseId,
    ICommandHandler<DeleteCourseCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        await handler.Handle(new DeleteCourseCommand(courseId), cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

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
                request.MobilePhone),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
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
                request.MobilePhone),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
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
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

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
                request.TeacherId,
                request.CourseId,
                request.WhatsAppGroupInviteUrl,
                request.WhatsAppNotifyPhones),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
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
                request.TeacherId,
                request.CourseId,
                request.WhatsAppGroupInviteUrl,
                request.WhatsAppNotifyPhones),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
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
        return ProblemFromException(ex);
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
            new UpdateClassroomAssignmentsCommand(classroomId, request.TeacherId, request.CourseId),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapPost("/api/classrooms/{classroomId:guid}/students", async (
    Guid classroomId,
    AddClassroomStudentRequest request,
    ICommandHandler<AddStudentToClassroomCommand, ClassroomDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new AddStudentToClassroomCommand(classroomId, request.StudentId),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

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
        return ProblemFromException(ex);
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
            new UpdateClassroomWhatsAppCommand(classroomId, request.WhatsAppGroupInviteUrl, request.WhatsAppNotifyPhones),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin,Teacher" });

app.MapGet("/api/lessons", async (
    Guid? courseId,
    IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetLessonsQuery(courseId), cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/lessons/{lessonId:guid}", async (
    Guid lessonId,
    IQueryHandler<GetLessonByIdQuery, LessonDto?> handler,
    CancellationToken cancellationToken) =>
{
    var lesson = await handler.Handle(new GetLessonByIdQuery(lessonId), cancellationToken);
    return lesson is null ? Results.NotFound() : Results.Ok(lesson);
}).RequireAuthorization();

app.MapPost("/api/progress/complete-step", async (
    CompleteStepRequest request,
    HttpContext httpContext,
    ICommandHandler<CompleteStepCommand, CompleteStepResponse> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var result = await handler.Handle(
            new CompleteStepCommand(userId, request.LessonId, request.StepId, request.SubmittedAnswer),
            cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/progress/me", async (
    HttpContext httpContext,
    IQueryHandler<GetStudentSummaryQuery, StudentSummaryDto> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new GetStudentSummaryQuery(userId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/quizzes", async (
    Guid? courseId,
    Guid? classroomId,
    IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetQuizzesQuery(courseId, classroomId), cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/quizzes/{quizId:guid}", async (
    Guid quizId,
    IQueryHandler<GetQuizByIdQuery, QuizDto?> handler,
    CancellationToken cancellationToken) =>
{
    var quiz = await handler.Handle(new GetQuizByIdQuery(quizId), cancellationToken);
    return quiz is null ? Results.NotFound() : Results.Ok(quiz);
}).RequireAuthorization();

app.MapPost("/api/quizzes", async (
    CreateQuizRequest request,
    HttpContext httpContext,
    ICommandHandler<CreateQuizCommand, QuizDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new CreateQuizCommand(
                userId,
                request.CourseId,
                request.ClassroomId,
                request.Title,
                request.Description,
                request.XpReward,
                request.Questions),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPost("/api/quizzes/submit", async (
    SubmitQuizRequest request,
    HttpContext httpContext,
    ICommandHandler<SubmitQuizCommand, SubmitQuizResponse> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var result = await handler.Handle(
            new SubmitQuizCommand(userId, request.QuizId, request.Answers),
            cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/assignments", async (
    Guid? classroomId,
    HttpContext httpContext,
    IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    return Results.Ok(await handler.Handle(new GetAssignmentsQuery(userId, role, classroomId), cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/assignments/{assignmentId:guid}", async (
    Guid assignmentId,
    HttpContext httpContext,
    IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    var assignment = await handler.Handle(new GetAssignmentByIdQuery(assignmentId, userId, role), cancellationToken);
    return assignment is null ? Results.NotFound() : Results.Ok(assignment);
}).RequireAuthorization();

app.MapPost("/api/assignments", async (
    CreateAssignmentRequest request,
    HttpContext httpContext,
    ICommandHandler<CreateAssignmentCommand, AssignmentDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new CreateAssignmentCommand(
                userId,
                request.ClassroomId,
                request.Title,
                request.Description,
                request.DueAtUtc,
                request.XpReward,
                request.Questions),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPost("/api/assignments/submit", async (
    SubmitAssignmentRequest request,
    HttpContext httpContext,
    ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new SubmitAssignmentCommand(userId, request.AssignmentId, request.Answers),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/assignments/{assignmentId:guid}/submissions", async (
    Guid assignmentId,
    HttpContext httpContext,
    IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetAssignmentSubmissionsQuery(userId, assignmentId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPost("/api/assignments/submissions/grade", async (
    GradeSubmissionRequest request,
    HttpContext httpContext,
    ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new GradeSubmissionCommand(userId, request.SubmissionId, request.TeacherFeedback, request.Answers),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/badges/me", async (
    HttpContext httpContext,
    IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new GetBadgesQuery(userId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/avatars", async (
    HttpContext httpContext,
    IQueryHandler<GetAvatarsQuery, IReadOnlyList<AvatarDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new GetAvatarsQuery(userId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapPost("/api/avatars/select", async (
    SelectAvatarRequest request,
    HttpContext httpContext,
    ICommandHandler<SelectAvatarCommand, AvatarDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var result = await handler.Handle(new SelectAvatarCommand(userId, request.AvatarId), cancellationToken);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/dashboard/parent", async (
    HttpContext httpContext,
    IQueryHandler<GetParentDashboardQuery, ParentDashboardDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetParentDashboardQuery(userId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Parent" });

app.MapGet("/api/dashboard/teacher", async (
    HttpContext httpContext,
    IQueryHandler<GetTeacherDashboardQuery, TeacherDashboardDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetTeacherDashboardQuery(userId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

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
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/zoom/status", async (
    HttpContext httpContext,
    IQueryHandler<GetZoomStatusQuery, ZoomConnectionStatus> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetZoomStatusQuery(userId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/zoom/connect", async (
    HttpContext httpContext,
    IQueryHandler<GetZoomConnectUrlQuery, ZoomConnectUrlDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetZoomConnectUrlQuery(userId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/zoom/callback", async (
    string? code,
    string? state,
    string? error,
    ICommandHandler<CompleteZoomConnectCommand, ZoomConnectResultDto> handler,
    CancellationToken cancellationToken) =>
{
    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.Redirect($"http://localhost:4200/teacher/zoom?zoom=error&message={Uri.EscapeDataString(error)}");
    }

    try
    {
        var result = await handler.Handle(
            new CompleteZoomConnectCommand(code ?? string.Empty, state ?? string.Empty),
            cancellationToken);
        return Results.Redirect(result.FrontendRedirectUrl);
    }
    catch (Exception ex)
    {
        return Results.Redirect(
            $"http://localhost:4200/teacher/zoom?zoom=error&message={Uri.EscapeDataString(ex.Message)}");
    }
});

app.MapPost("/api/zoom/disconnect", async (
    HttpContext httpContext,
    ICommandHandler<DisconnectZoomCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        await handler.Handle(new DisconnectZoomCommand(userId), cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.Run();
