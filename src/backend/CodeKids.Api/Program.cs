using System.Security.Claims;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using CodeKids.Application.Features.Admin;
using CodeKids.Application.Features.Analytics;
using CodeKids.Application.Features.Appointments;
using CodeKids.Application.Features.Assignments;
using CodeKids.Application.Features.Auth;
using CodeKids.Application.Features.Avatars;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.Dashboard;
using CodeKids.Application.Features.Exams;
using CodeKids.Application.Features.Lessons;
using CodeKids.Application.Features.Media;
using CodeKids.Application.Features.Meetings;
using CodeKids.Application.Features.Progress;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.Quizzes;
using CodeKids.Application.Features.SiteSettings;
using CodeKids.Application.Features.ZoomConnect;
using CodeKids.Application.Common;
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
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200"];

    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddMediaStorage(builder.Configuration);
builder.Services.AddZoomIntegration(builder.Configuration);
builder.Services.AddWhatsAppIntegration(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<ICommandHandler<RegisterCommand, AuthResponse>, RegisterCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>, GetCoursesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto>, GetSiteSettingsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto>, UpdateSiteSettingsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UploadSiteImageCommand, SiteSettingsDto>, UploadSiteImageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>>, CreateCourseCommandHandler>();
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
builder.Services.AddScoped<IQueryHandler<GetTeacherStudentDetailQuery, TeacherStudentDetailDto>, GetTeacherStudentDetailQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassroomDiagnosisQuery, ClassroomDiagnosisDto>, GetClassroomDiagnosisQueryHandler>();
builder.Services.AddScoped<ICommandHandler<RunDailyWhatsAppReportsCommand, DailyWhatsAppReportsResultDto>, RunDailyWhatsAppReportsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateMeetingCommand, LiveSessionDto>, CreateMeetingCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetMeetingsQuery, IReadOnlyList<LiveSessionDto>>, GetMeetingsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>>, ListAppointmentsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateAppointmentCommand, AppointmentDto>, CreateAppointmentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateAppointmentCommand, AppointmentDto>, UpdateAppointmentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteAppointmentCommand, bool>, DeleteAppointmentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetZoomConnectUrlQuery, ZoomConnectUrlDto>, GetZoomConnectUrlQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteZoomConnectCommand, ZoomConnectResultDto>, CompleteZoomConnectCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetZoomStatusQuery, ZoomConnectionStatus>, GetZoomStatusQueryHandler>();
builder.Services.AddScoped<ICommandHandler<DisconnectZoomCommand, bool>, DisconnectZoomCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetZoomOAuthSettingsQuery, ZoomUserOAuthSettingsDto>, GetZoomOAuthSettingsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SaveZoomOAuthSettingsCommand, ZoomUserOAuthSettingsDto>, SaveZoomOAuthSettingsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateManagedUserCommand, ManagedUserDto>, CreateManagedUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateManagedUserCommand, ManagedUserDto>, UpdateManagedUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteManagedUserCommand, bool>, DeleteManagedUserCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>, ListManagedUsersQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateClassroomCommand, ClassroomDto>, CreateClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomCommand, ClassroomDto>, UpdateClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteClassroomCommand, bool>, DeleteClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>, UpdateClassroomAssignmentsCommandHandler>();
builder.Services.AddScoped<ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto>, AddStudentToClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto>, RemoveStudentFromClassroomCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomWhatsAppCommand, ClassroomDto>, UpdateClassroomWhatsAppCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SendClassroomWhatsAppCommand, SendClassroomWhatsAppResultDto>, SendClassroomWhatsAppCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>>, GetClassroomsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetClassroomByIdQuery, ClassroomDto?>, GetClassroomByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateAssignmentCommand, AssignmentDto>, CreateAssignmentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>>, GetAssignmentsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?>, GetAssignmentByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto>, SubmitAssignmentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>>, GetAssignmentSubmissionsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto>, GradeSubmissionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateBankQuestionCommand, BankQuestionDto>, CreateBankQuestionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListBankQuestionsQuery, IReadOnlyList<BankQuestionDto>>, ListBankQuestionsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateBankQuestionCommand, BankQuestionDto>, UpdateBankQuestionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteBankQuestionCommand, bool>, DeleteBankQuestionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateExamCommand, ExamDto>, CreateExamCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>>, GetExamsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetExamByIdQuery, ExamDto?>, GetExamByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SubmitExamCommand, ExamAttemptDto>, SubmitExamCommandHandler>();
builder.Services.AddScoped<ICommandHandler<StartExamCommand, ExamAttemptDto>, StartExamCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>>, GetExamAttemptsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<AttachLessonVideoCommand, LessonVideoDto>, AttachLessonVideoCommandHandler>();
builder.Services.AddScoped<ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto>, AttachAssignmentSolutionVideoCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetLessonVideosQuery, IReadOnlyList<LessonVideoDto>>, GetLessonVideosQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto>, GetTeacherVideoLibraryQueryHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteLessonVideoCommand, bool>, DeleteLessonVideoCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool>, DeleteAssignmentSolutionVideoCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetPlaybackQuery, PlaybackDto>, GetPlaybackQueryHandler>();
builder.Services.AddScoped<ICommandHandler<RecordWatchEventsCommand, WatchSessionDto>, RecordWatchEventsCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetWatchSessionsQuery, IReadOnlyList<WatchSessionDto>>, GetWatchSessionsQueryHandler>();

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

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524288000;
});

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

static IResult ProblemFromException(Exception ex)
{
    if (ex is ApiException api)
    {
        return Results.BadRequest(new { code = api.Code, message = api.Message, args = api.Args });
    }

    var resolved = ApiErrorCatalog.TryResolve(ex.Message);
    if (resolved is not null)
    {
        return Results.BadRequest(new { code = resolved.Value.Code, message = ex.Message, args = resolved.Value.Args });
    }

    return Results.BadRequest(new { code = "api.errors.unknown", message = ex.Message });
}

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
    HttpContext httpContext,
    IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>> handler,
    CancellationToken cancellationToken) =>
{
    Guid? userId = null;
    string? role = null;
    try
    {
        userId = CurrentUser.GetUserId(httpContext.User);
        role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? httpContext.User.FindFirst("role")?.Value;
    }
    catch
    {
        // Authorized endpoint; ignore if claim missing.
    }

    return Results.Ok(await handler.Handle(new GetCoursesQuery(userId, role), cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/site-settings", async (
    IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetSiteSettingsQuery(), cancellationToken));
}).AllowAnonymous();

app.MapGet("/api/site-settings/logo", async (
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    CancellationToken cancellationToken) =>
{
    var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);
    if (string.IsNullOrWhiteSpace(settings.LogoStorageKey))
    {
        return Results.NotFound();
    }

    var stream = await fileStorage.OpenReadAsync(settings.LogoStorageKey, cancellationToken);
    return Results.File(stream, string.IsNullOrWhiteSpace(settings.LogoContentType) ? "image/png" : settings.LogoContentType);
}).AllowAnonymous();

app.MapGet("/api/site-settings/banner", async (
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    CancellationToken cancellationToken) =>
{
    var settings = await GetSiteSettingsQueryHandler.EnsureAsync(dbContext, cancellationToken);
    if (string.IsNullOrWhiteSpace(settings.BannerStorageKey))
    {
        return Results.NotFound();
    }

    var stream = await fileStorage.OpenReadAsync(settings.BannerStorageKey, cancellationToken);
    return Results.File(stream, string.IsNullOrWhiteSpace(settings.BannerContentType) ? "image/jpeg" : settings.BannerContentType);
}).AllowAnonymous();

app.MapPut("/api/admin/site-settings", async (
    UpdateSiteSettingsRequest request,
    HttpContext httpContext,
    ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var adminId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new UpdateSiteSettingsCommand(
                adminId,
                request.SiteName,
                request.ClearLogo == true,
                request.ClearBanner == true),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapPost("/api/admin/site-settings/upload", async (
    HttpRequest request,
    HttpContext httpContext,
    IFileStorage fileStorage,
    ICommandHandler<UploadSiteImageCommand, SiteSettingsDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { code = "api.errors.media.multipartRequired", message = "Expected multipart form upload." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "api.errors.media.noFile", message = "No file uploaded." });
        }

        var kind = form["kind"].ToString();
        if (string.IsNullOrWhiteSpace(kind))
        {
            kind = "logo";
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        if (file.Length > 5 * 1024 * 1024)
        {
            return Results.BadRequest(new { message = "Image must be 5 MB or smaller." });
        }

        var adminId = CurrentUser.GetUserId(httpContext.User);
        await using var stream = file.OpenReadStream();
        var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken);

        return Results.Ok(await handler.Handle(
            new UploadSiteImageCommand(adminId, kind, storageKey, contentType),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" })
    .DisableAntiforgery();

app.MapPost("/api/admin/courses", async (
    CreateCourseRequest request,
    ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new CreateCourseCommand(
                request.Title,
                request.Theme,
                request.Description,
                request.AgeMin,
                request.AgeMax,
                request.Term,
                request.Grades,
                request.SortOrder),
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
            new UpdateCourseCommand(
                courseId,
                request.Title,
                request.Theme,
                request.Description,
                request.AgeMin,
                request.AgeMax,
                request.Term,
                request.Grade,
                request.SortOrder),
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

app.MapGet("/api/admin/appointments", async (
    DateTimeOffset? fromUtc,
    DateTimeOffset? toUtc,
    IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new ListAppointmentsQuery(fromUtc, toUtc), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapPost("/api/admin/appointments", async (
    CreateAppointmentRequest request,
    ICommandHandler<CreateAppointmentCommand, AppointmentDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new CreateAppointmentCommand(
                request.TeacherId,
                request.CourseId,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.Notes),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapPut("/api/admin/appointments/{appointmentId:guid}", async (
    Guid appointmentId,
    UpdateAppointmentRequest request,
    ICommandHandler<UpdateAppointmentCommand, AppointmentDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new UpdateAppointmentCommand(
                appointmentId,
                request.TeacherId,
                request.CourseId,
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.Notes),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" });

app.MapDelete("/api/admin/appointments/{appointmentId:guid}", async (
    Guid appointmentId,
    ICommandHandler<DeleteAppointmentCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        await handler.Handle(new DeleteAppointmentCommand(appointmentId), cancellationToken);
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
                request.Grade,
                request.MobilePhone,
                request.WorkShift,
                request.Stages),
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
                request.Grade,
                request.MobilePhone,
                request.WorkShift,
                request.Stages),
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
                request.Grade,
                request.Courses,
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
                request.Grade,
                request.Courses,
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
            new UpdateClassroomAssignmentsCommand(classroomId, request.Courses),
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
    ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto> handler,
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
                request.IncludeGroupInviteLink),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
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
            new UpdateClassroomWhatsAppCommand(
                classroomId,
                request.WhatsAppGroupInviteUrl,
                request.WhatsAppNotifyPhones,
                request.DailyWhatsAppReportsEnabled),
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

app.MapGet("/api/question-bank", async (
    Guid? courseId,
    Guid? lessonId,
    HttpContext httpContext,
    IQueryHandler<ListBankQuestionsQuery, IReadOnlyList<BankQuestionDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new ListBankQuestionsQuery(userId, courseId, lessonId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPost("/api/question-bank", async (
    CreateBankQuestionRequest request,
    HttpContext httpContext,
    ICommandHandler<CreateBankQuestionCommand, BankQuestionDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new CreateBankQuestionCommand(
                userId,
                request.CourseId,
                request.LessonId,
                request.QuestionType,
                request.Prompt,
                request.PassageText,
                request.OptionA,
                request.OptionB,
                request.OptionC,
                request.OptionD,
                request.Options,
                request.CorrectAnswer,
                request.Points,
                request.SortOrder,
                request.Children),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPut("/api/question-bank/{questionId:guid}", async (
    Guid questionId,
    UpdateBankQuestionRequest request,
    HttpContext httpContext,
    ICommandHandler<UpdateBankQuestionCommand, BankQuestionDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new UpdateBankQuestionCommand(
                userId,
                questionId,
                request.LessonId,
                request.Prompt,
                request.PassageText,
                request.OptionA,
                request.OptionB,
                request.OptionC,
                request.OptionD,
                request.Options,
                request.CorrectAnswer,
                request.Points,
                request.SortOrder),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapDelete("/api/question-bank/{questionId:guid}", async (
    Guid questionId,
    HttpContext httpContext,
    ICommandHandler<DeleteBankQuestionCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        await handler.Handle(new DeleteBankQuestionCommand(userId, questionId), cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/exams", async (
    Guid? classroomId,
    HttpContext httpContext,
    IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    return Results.Ok(await handler.Handle(new GetExamsQuery(userId, role, classroomId), cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/exams/{examId:guid}", async (
    Guid examId,
    HttpContext httpContext,
    IQueryHandler<GetExamByIdQuery, ExamDto?> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    var exam = await handler.Handle(new GetExamByIdQuery(examId, userId, role), cancellationToken);
    return exam is null ? Results.NotFound() : Results.Ok(exam);
}).RequireAuthorization();

app.MapPost("/api/exams", async (
    CreateExamRequest request,
    HttpContext httpContext,
    ICommandHandler<CreateExamCommand, ExamDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new CreateExamCommand(
                userId,
                request.ClassroomId,
                request.CourseId,
                request.Title,
                request.Description,
                request.DueAtUtc,
                request.XpReward,
                request.QuestionIds),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapPost("/api/exams/{examId:guid}/start", async (
    Guid examId,
    HttpContext httpContext,
    ICommandHandler<StartExamCommand, ExamAttemptDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new StartExamCommand(userId, examId), cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapPost("/api/exams/submit", async (
    SubmitExamRequest request,
    HttpContext httpContext,
    ICommandHandler<SubmitExamCommand, ExamAttemptDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new SubmitExamCommand(userId, request.ExamId, request.Answers),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapPost("/api/media/upload", async (
    HttpRequest request,
    HttpContext httpContext,
    IFileStorage fileStorage,
    IAppDbContext dbContext,
    Microsoft.Extensions.Options.IOptions<MediaOptions> mediaOptions,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { code = "api.errors.media.multipartRequired", message = "Expected multipart form upload." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "api.errors.media.noFile", message = "No file uploaded." });
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        MediaUploadRules.EnsureAllowed(contentType, file.Length, mediaOptions.Value.MaxUploadBytes);

        int? durationSeconds = null;
        if (int.TryParse(form["durationSeconds"], out var parsedDuration) && parsedDuration > 0)
        {
            durationSeconds = parsedDuration;
        }

        var userId = CurrentUser.GetUserId(httpContext.User);
        await using var stream = file.OpenReadStream();
        var storageKey = await fileStorage.SaveAsync(stream, file.FileName, contentType, cancellationToken);

        var asset = new CodeKids.Domain.Entities.MediaAsset
        {
            Id = Guid.NewGuid(),
            StorageKey = storageKey,
            FileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = file.Length,
            DurationSeconds = durationSeconds,
            UploadedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new MediaAssetDto(
            asset.Id,
            asset.FileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.DurationSeconds,
            asset.CreatedAtUtc));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" })
    .DisableAntiforgery();

app.MapPost("/api/lessons/{lessonId:guid}/videos", async (
    Guid lessonId,
    AttachLessonVideoRequest request,
    HttpContext httpContext,
    ICommandHandler<AttachLessonVideoCommand, LessonVideoDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new AttachLessonVideoCommand(userId, lessonId, request.MediaAssetId, request.Title, request.SortOrder),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapGet("/api/lessons/{lessonId:guid}/videos", async (
    Guid lessonId,
    IQueryHandler<GetLessonVideosQuery, IReadOnlyList<LessonVideoDto>> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetLessonVideosQuery(lessonId), cancellationToken));
}).RequireAuthorization();

app.MapPost("/api/assignments/{assignmentId:guid}/solution-video", async (
    Guid assignmentId,
    AttachLessonVideoRequest request,
    HttpContext httpContext,
    ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new AttachAssignmentSolutionVideoCommand(userId, assignmentId, request.MediaAssetId),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/media/library", async (
    HttpContext httpContext,
    IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new GetTeacherVideoLibraryQuery(userId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapDelete("/api/lessons/videos/{lessonVideoId:guid}", async (
    Guid lessonVideoId,
    HttpContext httpContext,
    ICommandHandler<DeleteLessonVideoCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        await handler.Handle(new DeleteLessonVideoCommand(userId, lessonVideoId), cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapDelete("/api/assignments/{assignmentId:guid}/solution-video", async (
    Guid assignmentId,
    HttpContext httpContext,
    ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        await handler.Handle(new DeleteAssignmentSolutionVideoCommand(userId, assignmentId), cancellationToken);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapGet("/api/media/{mediaAssetId:guid}/playback", async (
    Guid mediaAssetId,
    HttpContext httpContext,
    IQueryHandler<GetPlaybackQuery, PlaybackDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        var baseApiUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api";
        return Results.Ok(await handler.Handle(
            new GetPlaybackQuery(mediaAssetId, userId, baseApiUrl),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization();

app.MapGet("/api/media/stream", async (
    string token,
    IMediaAccessTokenService tokenService,
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    CancellationToken cancellationToken) =>
{
    if (!tokenService.TryValidate(token, out var mediaAssetId, out _, out _))
    {
        return Results.Unauthorized();
    }

    var media = await dbContext.MediaAssets.AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
    if (media is null)
    {
        return Results.NotFound();
    }

    var stream = await fileStorage.OpenReadAsync(media.StorageKey, cancellationToken);
    return Results.File(
        stream,
        contentType: media.ContentType,
        fileDownloadName: null,
        enableRangeProcessing: true);
}).AllowAnonymous();

app.MapPost("/api/media/watch-events", async (
    RecordWatchEventsRequest request,
    HttpContext httpContext,
    ICommandHandler<RecordWatchEventsCommand, WatchSessionDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new RecordWatchEventsCommand(
                userId,
                request.MediaAssetId,
                request.LessonId,
                request.SessionId,
                request.Events),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Student" });

app.MapGet("/api/media/{mediaAssetId:guid}/watch-sessions", async (
    Guid mediaAssetId,
    HttpContext httpContext,
    IQueryHandler<GetWatchSessionsQuery, IReadOnlyList<WatchSessionDto>> handler,
    CancellationToken cancellationToken) =>
{
    var userId = CurrentUser.GetUserId(httpContext.User);
    return Results.Ok(await handler.Handle(new GetWatchSessionsQuery(userId, mediaAssetId), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapGet("/api/exams/{examId:guid}/attempts", async (
    Guid examId,
    HttpContext httpContext,
    IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(new GetExamAttemptsQuery(userId, examId), cancellationToken));
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

app.MapGet("/api/dashboard/teacher/students/{studentId:guid}", async (
    Guid studentId,
    HttpContext httpContext,
    IQueryHandler<GetTeacherStudentDetailQuery, TeacherStudentDetailDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new GetTeacherStudentDetailQuery(userId, studentId),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

app.MapGet("/api/dashboard/teacher/classrooms/{classroomId:guid}/diagnosis", async (
    Guid classroomId,
    HttpContext httpContext,
    IQueryHandler<GetClassroomDiagnosisQuery, ClassroomDiagnosisDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var userId = CurrentUser.GetUserId(httpContext.User);
        return Results.Ok(await handler.Handle(
            new GetClassroomDiagnosisQuery(userId, classroomId),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher" });

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
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

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

app.MapGet("/api/zoom/oauth-settings", async (
    IQueryHandler<GetZoomOAuthSettingsQuery, ZoomUserOAuthSettingsDto> handler,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(await handler.Handle(new GetZoomOAuthSettingsQuery(), cancellationToken));
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.MapPut("/api/zoom/oauth-settings", async (
    SaveZoomUserOAuthSettingsRequest request,
    ICommandHandler<SaveZoomOAuthSettingsCommand, ZoomUserOAuthSettingsDto> handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await handler.Handle(
            new SaveZoomOAuthSettingsCommand(
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                request.FrontendRedirectUri),
            cancellationToken));
    }
    catch (Exception ex)
    {
        return ProblemFromException(ex);
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Teacher,SuperAdmin" });

app.Run();
