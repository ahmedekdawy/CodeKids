using CodeKids.Api;

using CodeKids.Application.Abstractions;

using CodeKids.Application.Features.Admin;

using CodeKids.Application.Features.Analytics;

using CodeKids.Application.Features.Appointments;

using CodeKids.Application.Features.Assignments;

using CodeKids.Application.Features.Attendance;

using CodeKids.Application.Features.Auth;

using CodeKids.Application.Features.Avatars;

using CodeKids.Application.Features.Badges;

using CodeKids.Application.Features.Classrooms;

using CodeKids.Application.Features.Courses;

using CodeKids.Application.Features.Dashboard;

using CodeKids.Application.Features.Exams;

using CodeKids.Application.Features.Expenses;

using CodeKids.Application.Features.Lessons;

using CodeKids.Application.Features.Media;

using CodeKids.Application.Features.Meetings;

using CodeKids.Application.Features.Payments;

using CodeKids.Application.Features.Progress;

using CodeKids.Application.Features.QuestionBank;

using CodeKids.Application.Features.Quizzes;

using CodeKids.Application.Features.Reports;

using CodeKids.Application.Features.SiteSettings;

using CodeKids.Application.Features.Timetable;

using CodeKids.Application.Features.ZoomConnect;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;

using System.Security.Claims;

using System.Text;

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

builder.Services.AddEmailSender(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<ICommandHandler<RegisterCommand, AuthResponse>, RegisterCommandHandler>();

builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>, ForgotPasswordCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>, GetCoursesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto>, GetSiteSettingsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto>, UpdateSiteSettingsCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UploadSiteImageCommand, SiteSettingsDto>, UploadSiteImageCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>>, CreateCourseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateCourseCommand, CourseSummaryDto>, UpdateCourseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteCourseCommand, bool>, DeleteCourseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateCourseUnitCommand, CourseUnitDto>, CreateCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCourseUnitCommand, CourseUnitDto>, UpdateCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCourseUnitCommand, bool>, DeleteCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCourseLessonCommand, CourseLessonDto>, CreateCourseLessonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCourseLessonCommand, CourseLessonDto>, UpdateCourseLessonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCourseLessonCommand, bool>, DeleteCourseLessonCommandHandler>();

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

builder.Services.AddScoped<ICommandHandler<CreateAppointmentCommand, CreateAppointmentsResult>, CreateAppointmentCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateAppointmentCommand, AppointmentDto>, UpdateAppointmentCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteAppointmentCommand, bool>, DeleteAppointmentCommandHandler>();

builder.Services.AddScoped<IQueryHandler<ListFixedTimetableEntriesQuery, IReadOnlyList<FixedTimetableEntryDto>>, ListFixedTimetableEntriesQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateFixedTimetableEntryCommand, FixedTimetableEntryDto>, CreateFixedTimetableEntryCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateFixedTimetableEntryCommand, FixedTimetableEntryDto>, UpdateFixedTimetableEntryCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteFixedTimetableEntryCommand, bool>, DeleteFixedTimetableEntryCommandHandler>();

builder.Services.AddScoped<IQueryHandler<ListTeacherSessionAttendanceQuery, IReadOnlyList<TeacherSessionAttendanceDto>>, ListTeacherSessionAttendanceQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateTeacherSessionAttendanceCommand, TeacherSessionAttendanceDto>, CreateTeacherSessionAttendanceCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteTeacherSessionAttendanceCommand, bool>, DeleteTeacherSessionAttendanceCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetTeacherPayrollReportQuery, TeacherPayrollReportDto>, GetTeacherPayrollReportQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetAccountReportQuery, AccountReportDto>, GetAccountReportQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListTuitionPaymentsQuery, IReadOnlyList<TuitionPaymentDto>>, ListTuitionPaymentsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateTuitionPaymentCommand, TuitionPaymentDto>, CreateTuitionPaymentCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteTuitionPaymentCommand, bool>, DeleteTuitionPaymentCommandHandler>();

builder.Services.AddScoped<IQueryHandler<ListOtherExpensesQuery, IReadOnlyList<OtherExpenseDto>>, ListOtherExpensesQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateOtherExpenseCommand, OtherExpenseDto>, CreateOtherExpenseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteOtherExpenseCommand, bool>, DeleteOtherExpenseCommandHandler>();

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

builder.Services.AddScoped<ICommandHandler<RegisterMediaFromUrlCommand, MediaAssetDto>, RegisterMediaFromUrlCommandHandler>();

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

    // Legacy schema (EnsureCreated + additive SQL) for existing tables.

    await SchemaBootstrap.EnsureAsync(dbContext);

    // New schema changes ship as EF Core migrations (e.g. PasswordResetTokens).

    await dbContext.Database.MigrateAsync();

    await DataSeeder.SeedAsync(dbContext, passwordHasher);

}

app.MapAccountReportEndpoints();

app.MapAdminUsersEndpoints();

app.MapAppointmentsEndpoints();

app.MapAssignmentsEndpoints();

app.MapAttendanceEndpoints();

app.MapAuthEndpoints();

app.MapAvatarsEndpoints();

app.MapBadgesEndpoints();

app.MapClassroomsEndpoints();

app.MapCoursesEndpoints();
app.MapCourseTreeEndpoints();

app.MapDashboardEndpoints();

app.MapExamsEndpoints();

app.MapExpensesEndpoints();

app.MapLessonsEndpoints();

app.MapMediaEndpoints();

app.MapMeetingsEndpoints();

app.MapPaymentsEndpoints();

app.MapProgressEndpoints();

app.MapQuestionBankEndpoints();

app.MapQuizzesEndpoints();

app.MapReportsEndpoints();

app.MapSiteSettingsEndpoints();

app.MapTimetableEndpoints();

app.MapZoomEndpoints();

app.Run();
