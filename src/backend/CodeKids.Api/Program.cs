using CodeKids.Api;

using CodeKids.Application.Abstractions;

using CodeKids.Application.Features.Admin;

using CodeKids.Application.Features.Analytics;

using CodeKids.Application.Features.Appointments;

using CodeKids.Application.Features.Assessments;

using CodeKids.Application.Features.Assignments;

using CodeKids.Application.Features.Attendance;
using CodeKids.Application.Features.StudentAttendance;

using CodeKids.Application.Features.Auth;

using CodeKids.Application.Features.Tenants;

using CodeKids.Application.Features.Avatars;

using CodeKids.Application.Features.Badges;

using CodeKids.Application.Features.Classrooms;

using CodeKids.Application.Features.Courses;

using CodeKids.Application.Features.Dashboard;

using CodeKids.Application.Features.Exams;

using CodeKids.Application.Features.Expenses;

using CodeKids.Application.Features.Grades;

using CodeKids.Application.Features.Subjects;

using CodeKids.Application.Features.Lessons;

using CodeKids.Application.Features.Media;

using CodeKids.Application.Features.Meetings;

using CodeKids.Application.Features.Payments;

using CodeKids.Application.Features.Profile;

using CodeKids.Application.Features.Progress;

using CodeKids.Application.Features.QuestionBank;

using CodeKids.Application.Features.Quizzes;

using CodeKids.Application.Features.Reports;

using CodeKids.Application.Features.SiteSettings;

using CodeKids.Application.Features.StudyPlans;

using CodeKids.Application.Features.StudentAsk;

using CodeKids.Application.Features.Chat;

using CodeKids.Application.Features.Notifications;

using CodeKids.Api.Hubs;

using CodeKids.Application.Features.Timetable;

using CodeKids.Application.Features.WeeklyReports;

using CodeKids.Application.Features.ZoomConnect;

using CodeKids.Domain.Abstractions;

using CodeKids.Infrastructure;
using CodeKids.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;

using System.Security.Claims;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

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

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<TenantCatalog>();

builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

builder.Services.AddCors(options =>

{

    var catalog = new TenantCatalog(builder.Configuration);

    var allowed = CorsOrigins.Resolve(builder.Configuration, catalog);

    options.AddPolicy("frontend", policy =>

        policy.SetIsOriginAllowed(origin => CorsOrigins.IsAllowed(allowed, origin))

            .AllowAnyHeader()

            .AllowAnyMethod()

            .AllowCredentials()

            .SetPreflightMaxAge(TimeSpan.FromHours(1)));

});

builder.Services.AddDbContext<AppDbContext>((sp, options) =>

{

    var catalog = sp.GetRequiredService<TenantCatalog>();

    var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;

    var tenant = TenantRequest.Resolve(http, catalog);

    options.UseNpgsql(tenant.ConnectionString);

});

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddMediaStorage(builder.Configuration);

builder.Services.AddZoomIntegration(builder.Configuration);

builder.Services.AddWhatsAppIntegration(builder.Configuration);

builder.Services.AddEmailSender(builder.Configuration);

builder.Services.AddStudyPlanAi(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<ICommandHandler<RegisterCommand, AuthResponse>, RegisterCommandHandler>();

builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ImpersonateUserCommand, AuthResponse>, ImpersonateUserCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>, ForgotPasswordCommandHandler>();

builder.Services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateOwnAccountCommand, AuthUserDto>, UpdateOwnAccountCommandHandler>();

builder.Services.AddScoped<ICommandHandler<RegisterTenantCommand, RegisterTenantResult>, RegisterTenantCommandHandler>();

builder.Services.AddScoped<ICommandHandler<VerifyTenantCommand, VerifyTenantResult>, VerifyTenantCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>, GetCoursesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetCourseByIdQuery, CourseDto?>, GetCourseByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListAdminCoursesQuery, PagedCoursesResultDto>, ListAdminCoursesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListStagesQuery, IReadOnlyList<StageDto>>, ListStagesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListGradesQuery, IReadOnlyList<GradeDto>>, ListGradesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListSubjectsQuery, IReadOnlyList<SubjectDto>>, ListSubjectsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetSiteSettingsQuery, SiteSettingsDto>, GetSiteSettingsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateSiteSettingsCommand, SiteSettingsDto>, UpdateSiteSettingsCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UploadSiteImageCommand, SiteSettingsDto>, UploadSiteImageCommandHandler>();

builder.Services.AddScoped<ICommandHandler<SaveProfilePhotoCommand, AuthUserDto>, SaveProfilePhotoCommandHandler>();

builder.Services.AddScoped<ICommandHandler<RemoveProfilePhotoCommand, AuthUserDto>, RemoveProfilePhotoCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>>, CreateCourseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateCourseCommand, CourseSummaryDto>, UpdateCourseCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetCoursePublishedCommand, CourseSummaryDto>, SetCoursePublishedCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteCourseCommand, bool>, DeleteCourseCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateCourseUnitCommand, CourseUnitDto>, CreateCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCourseUnitCommand, CourseUnitDto>, UpdateCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCourseUnitCommand, bool>, DeleteCourseUnitCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCourseLessonCommand, CourseLessonDto>, CreateCourseLessonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCourseLessonCommand, CourseLessonDto>, UpdateCourseLessonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCourseLessonCommand, bool>, DeleteCourseLessonCommandHandler>();
builder.Services.AddScoped<ICommandHandler<GenerateCourseTreeCommand, GenerateCourseTreeResult>, GenerateCourseTreeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetStudentAskEnabledCommand, StudentAskSettingsDto>, SetStudentAskEnabledCommandHandler>();
builder.Services.AddScoped<ICommandHandler<AskStudentQuestionCommand, StudentAskAnswerDto>, AskStudentQuestionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListStudentAskedQuestionsQuery, IReadOnlyList<StudentAskedQuestionDto>>, ListStudentAskedQuestionsQueryHandler>();
builder.Services.AddScoped<ICommandHandler<AnswerStudentAskedQuestionCommand, StudentAskedQuestionDto>, AnswerStudentAskedQuestionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateStudentAskedQuestionCommand, StudentAskedQuestionDto>, UpdateStudentAskedQuestionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteStudentAskedQuestionCommand, bool>, DeleteStudentAskedQuestionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateChatRoomCommand, ChatRoomDto>, CreateChatRoomCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListChatRoomsQuery, IReadOnlyList<ChatRoomDto>>, ListChatRoomsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListChatMessagesQuery, IReadOnlyList<ChatMessageDto>>, ListChatMessagesQueryHandler>();
builder.Services.AddScoped<ICommandHandler<SendChatMessageCommand, ChatMessageDto>, SendChatMessageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteChatMessageCommand, ChatMessageDto>, DeleteChatMessageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetChatMemberBlockedCommand, ChatMemberDto>, SetChatMemberBlockedCommandHandler>();
builder.Services.AddScoped<ICommandHandler<MarkChatRoomReadCommand, int>, MarkChatRoomReadCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetChatUnreadSummaryQuery, ChatUnreadSummaryDto>, GetChatUnreadSummaryQueryHandler>();

builder.Services.AddScoped<INotificationRealtime, NotificationRealtime>();
builder.Services.AddScoped<NotificationPublisher>();
builder.Services.AddScoped<IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>, ListNotificationsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetNotificationUnreadSummaryQuery, NotificationUnreadSummaryDto>, GetNotificationUnreadSummaryQueryHandler>();
builder.Services.AddScoped<ICommandHandler<MarkNotificationReadCommand, NotificationDto>, MarkNotificationReadCommandHandler>();
builder.Services.AddScoped<ICommandHandler<MarkAllNotificationsReadCommand, int>, MarkAllNotificationsReadCommandHandler>();
builder.Services.AddSignalR();

builder.Services.AddScoped<IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>, GetLessonsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetLessonByIdQuery, LessonDto?>, GetLessonByIdQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CompleteStepCommand, CompleteStepResponse>, CompleteStepCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetStudentSummaryQuery, StudentSummaryDto>, GetStudentSummaryQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>>, GetQuizzesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetQuizByIdQuery, QuizDto?>, GetQuizByIdQueryHandler>();

builder.Services.AddScoped<ICommandHandler<SubmitQuizCommand, SubmitQuizResponse>, SubmitQuizCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateQuizCommand, QuizDto>, CreateQuizCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateQuizCommand, QuizDto>, UpdateQuizCommandHandler>();

builder.Services.AddScoped<ICommandHandler<PublishQuizCommand, QuizDto>, PublishQuizCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteQuizCommand, bool>, DeleteQuizCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetTeacherQuizzesQuery, IReadOnlyList<TeacherQuizListDto>>, GetTeacherQuizzesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetTeacherQuizByIdQuery, TeacherQuizDetailDto?>, GetTeacherQuizByIdQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetQuizAttemptsQuery, IReadOnlyList<QuizAttemptReviewDto>>, GetQuizAttemptsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetBadgesQuery, IReadOnlyList<BadgeDto>>, GetBadgesQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetAvatarsQuery, IReadOnlyList<AvatarDto>>, GetAvatarsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<SelectAvatarCommand, AvatarDto>, SelectAvatarCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>, GetParentDashboardQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetParentChildOverviewQuery, ParentChildOverviewDto>, GetParentChildOverviewQueryHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateParentManagedAccountCommand, ParentManagedAccountDto>, UpdateParentManagedAccountCommandHandler>();

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

builder.Services.AddScoped<IQueryHandler<ListStudentClassroomAttendanceQuery, PagedStudentClassroomAttendanceResultDto>, ListStudentClassroomAttendanceQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateStudentClassroomAttendanceCommand, StudentClassroomAttendanceDto>, CreateStudentClassroomAttendanceCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteStudentClassroomAttendanceCommand, bool>, DeleteStudentClassroomAttendanceCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetTeacherPayrollReportQuery, TeacherPayrollReportDto>, GetTeacherPayrollReportQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListTeacherPayrollAdjustmentsQuery, IReadOnlyList<TeacherPayrollAdjustmentDto>>, ListTeacherPayrollAdjustmentsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateTeacherPayrollAdjustmentCommand, TeacherPayrollAdjustmentDto>, CreateTeacherPayrollAdjustmentCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteTeacherPayrollAdjustmentCommand, bool>, DeleteTeacherPayrollAdjustmentCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetAccountReportQuery, AccountReportDto>, GetAccountReportQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetAdminLoginDashboardQuery, AdminLoginDashboardDto>, GetAdminLoginDashboardQueryHandler>();

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
builder.Services.AddScoped<ICommandHandler<SetManagedUserActiveCommand, ManagedUserDto>, SetManagedUserActiveCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SendAdminWhatsAppCommand, SendAdminWhatsAppResultDto>, SendAdminWhatsAppCommandHandler>();

builder.Services.AddScoped<IQueryHandler<ListManagedUsersQuery, IReadOnlyList<ManagedUserDto>>, ListManagedUsersQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateClassroomCommand, ClassroomDto>, CreateClassroomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateClassroomCommand, ClassroomDto>, UpdateClassroomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteClassroomCommand, bool>, DeleteClassroomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateClassroomAssignmentsCommand, ClassroomDto>, UpdateClassroomAssignmentsCommandHandler>();

builder.Services.AddScoped<ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto>, AddStudentToClassroomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<RemoveStudentFromClassroomCommand, ClassroomDto>, RemoveStudentFromClassroomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<UpdateClassroomWhatsAppCommand, ClassroomDto>, UpdateClassroomWhatsAppCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateClassroomZoomCommand, ClassroomDto>, UpdateClassroomZoomCommandHandler>();

builder.Services.AddScoped<ICommandHandler<SendClassroomWhatsAppCommand, SendClassroomWhatsAppResultDto>, SendClassroomWhatsAppCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>>, GetClassroomsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListClassroomEnrollmentsQuery, PagedClassroomEnrollmentsResultDto>, ListClassroomEnrollmentsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetClassroomByIdQuery, ClassroomDto?>, GetClassroomByIdQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateAssignmentCommand, AssignmentDto>, CreateAssignmentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateAssignmentCommand, AssignmentDto>, UpdateAssignmentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PublishAssignmentCommand, AssignmentDto>, PublishAssignmentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteAssignmentCommand, bool>, DeleteAssignmentCommandHandler>();

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
builder.Services.AddScoped<ICommandHandler<PublishExamCommand, ExamDto>, PublishExamCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>>, GetExamsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetExamByIdQuery, ExamDto?>, GetExamByIdQueryHandler>();

builder.Services.AddScoped<ICommandHandler<SubmitExamCommand, ExamAttemptDto>, SubmitExamCommandHandler>();

builder.Services.AddScoped<ICommandHandler<StartExamCommand, ExamAttemptDto>, StartExamCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>>, GetExamAttemptsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<GradeExamAttemptCommand, ExamAttemptDto>, GradeExamAttemptCommandHandler>();

builder.Services.AddScoped<ICommandHandler<AttachLessonVideoCommand, LessonVideoDto>, AttachLessonVideoCommandHandler>();

builder.Services.AddScoped<ICommandHandler<RegisterMediaFromUrlCommand, MediaAssetDto>, RegisterMediaFromUrlCommandHandler>();

builder.Services.AddScoped<ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto>, AttachAssignmentSolutionVideoCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetLessonVideosQuery, IReadOnlyList<LessonVideoDto>>, GetLessonVideosQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto>, GetTeacherVideoLibraryQueryHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteLessonVideoCommand, bool>, DeleteLessonVideoCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetCourseVideoLibraryQuery, IReadOnlyList<CourseVideoLibraryItemDto>>, GetCourseVideoLibraryQueryHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool>, DeleteAssignmentSolutionVideoCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetPlaybackQuery, PlaybackDto>, GetPlaybackQueryHandler>();

builder.Services.AddScoped<ICommandHandler<RecordWatchEventsCommand, WatchSessionDto>, RecordWatchEventsCommandHandler>();

builder.Services.AddScoped<IQueryHandler<GetWatchSessionsQuery, IReadOnlyList<WatchSessionDto>>, GetWatchSessionsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetWeeklyReportGridQuery, IReadOnlyList<StudentWeeklyReportGridRowDto>>, GetWeeklyReportGridQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListStudentWeeklyReportsQuery, IReadOnlyList<StudentWeeklyReportDto>>, ListStudentWeeklyReportsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<ListTopWeeklyStudentsQuery, IReadOnlyList<TopWeeklyStudentDto>>, ListTopWeeklyStudentsQueryHandler>();

builder.Services.AddScoped<ICommandHandler<SaveWeeklyReportsCommand, IReadOnlyList<StudentWeeklyReportGridRowDto>>, SaveWeeklyReportsCommandHandler>();

builder.Services.AddScoped<IQueryHandler<ListWeeklyStudyPlansQuery, IReadOnlyList<WeeklyStudyPlanDto>>, ListWeeklyStudyPlansQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListAdminWeeklyStudyPlansQuery, PagedWeeklyStudyPlansResultDto>, ListAdminWeeklyStudyPlansQueryHandler>();

builder.Services.AddScoped<ICommandHandler<SaveWeeklyStudyPlanCommand, WeeklyStudyPlanDto>, SaveWeeklyStudyPlanCommandHandler>();

builder.Services.AddScoped<ICommandHandler<GenerateWeeklyStudyPlanCommand, GenerateWeeklyStudyPlanResult>, GenerateWeeklyStudyPlanCommandHandler>();

builder.Services.AddScoped<ICommandHandler<GenerateAssessmentDraftCommand, GeneratedAssessmentDraftDto>, GenerateAssessmentDraftCommandHandler>();

builder.Services.AddScoped<ICommandHandler<DeleteWeeklyStudyPlanCommand, bool>, DeleteWeeklyStudyPlanCommandHandler>();

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

        options.Events = new JwtBearerEvents

        {

            OnMessageReceived = context =>

            {

                var accessToken = context.Request.Query["access_token"].ToString();

                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))

                {

                    context.Token = accessToken;

                }

                return Task.CompletedTask;

            },

            OnTokenValidated = async context =>

            {

                try

                {

                    var userId = CurrentUser.GetUserId(context.Principal!);

                    var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();

                    var isActive = await db.Users.AsNoTracking()

                        .Where(x => x.Id == userId)

                        .Select(x => (bool?)x.IsActive)

                        .FirstOrDefaultAsync();

                    if (isActive != true)

                    {

                        context.Fail("This account is inactive.");

                    }

                }

                catch

                {

                    context.Fail("This account is inactive.");

                }

            }

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

app.UseCors("frontend");

app.UseSwagger();

app.UseSwaggerUI();

app.UseAuthentication();

app.UseAuthorization();

//using (var scope = app.Services.CreateScope())

//{

//    var catalog = scope.ServiceProvider.GetRequiredService<TenantCatalog>();

//    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

//    foreach (var tenant in catalog.All)

//    {

//        var options = new DbContextOptionsBuilder<AppDbContext>()

//            .UseNpgsql(tenant.ConnectionString)

//            .Options;

//        await using var dbContext = new AppDbContext(options);

//        await TenantSchema.EnsureAsync(dbContext);

//       await DataSeeder.SeedAsync(dbContext, passwordHasher);

//    }

//}


app.MapAccountReportEndpoints();

app.MapAdminUsersEndpoints();

app.MapAppointmentsEndpoints();

app.MapAssessmentsEndpoints();

app.MapAssignmentsEndpoints();

app.MapAttendanceEndpoints();

app.MapAuthEndpoints();

app.MapTenantEndpoints();

app.MapAvatarsEndpoints();

app.MapBadgesEndpoints();

app.MapClassroomsEndpoints();

app.MapCoursesEndpoints();
app.MapCourseTreeEndpoints();
app.MapGradesEndpoints();
app.MapSubjectsEndpoints();

app.MapDashboardEndpoints();

app.MapParentEndpoints();

app.MapExamsEndpoints();

app.MapExpensesEndpoints();

app.MapLessonsEndpoints();
app.MapStudentAskEndpoints();

app.MapChatEndpoints();

app.MapNotificationEndpoints();

app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

app.MapMediaEndpoints();

app.MapQuestionImageEndpoints();

app.MapProfilePhotoEndpoints();

app.MapMeetingsEndpoints();

app.MapPaymentsEndpoints();

app.MapProgressEndpoints();

app.MapQuestionBankEndpoints();

app.MapQuizzesEndpoints();

app.MapReportsEndpoints();

app.MapSiteSettingsEndpoints();

app.MapTimetableEndpoints();

app.MapWeeklyReportsEndpoints();

app.MapStudyPlansEndpoints();

app.MapZoomEndpoints();

app.Run();
