using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;

namespace CodeKids.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        CurrentTenantId = tenantContext?.TenantId;
    }

    /// <summary>Current request tenant. Null means only shared rows (TenantId IS NULL) are visible.</summary>
    public string? CurrentTenantId { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<CourseUnit> CourseUnits => Set<CourseUnit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonStep> LessonSteps => Set<LessonStep>();
    public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<FixedTimetableEntry> FixedTimetableEntries => Set<FixedTimetableEntry>();
    public DbSet<TeacherSessionAttendance> TeacherSessionAttendances => Set<TeacherSessionAttendance>();
    public DbSet<StudentWeeklyReport> StudentWeeklyReports => Set<StudentWeeklyReport>();
    public DbSet<WeeklyStudyPlan> WeeklyStudyPlans => Set<WeeklyStudyPlan>();
    public DbSet<WeeklyStudyPlanItem> WeeklyStudyPlanItems => Set<WeeklyStudyPlanItem>();
    public DbSet<WeeklyStudyPlanTopic> WeeklyStudyPlanTopics => Set<WeeklyStudyPlanTopic>();
    public DbSet<TuitionPayment> TuitionPayments => Set<TuitionPayment>();
    public DbSet<OtherExpense> OtherExpenses => Set<OtherExpense>();
    public DbSet<TeacherPayrollAdjustment> TeacherPayrollAdjustments => Set<TeacherPayrollAdjustment>();
    public DbSet<TeacherCourseRate> TeacherCourseRates => Set<TeacherCourseRate>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<ClassroomCourse> ClassroomCourses => Set<ClassroomCourse>();
    public DbSet<ClassroomStudent> ClassroomStudents => Set<ClassroomStudent>();
    public DbSet<StudentCourseEnrollment> StudentCourseEnrollments => Set<StudentCourseEnrollment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentQuestion> AssignmentQuestions => Set<AssignmentQuestion>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<AssignmentAnswer> AssignmentAnswers => Set<AssignmentAnswer>();
    public DbSet<BankQuestion> BankQuestions => Set<BankQuestion>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<LessonVideo> LessonVideos => Set<LessonVideo>();
    public DbSet<VideoWatchSession> VideoWatchSessions => Set<VideoWatchSession>();
    public DbSet<WhatsAppReportLog> WhatsAppReportLogs => Set<WhatsAppReportLog>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<TenantSignup> TenantSignups => Set<TenantSignup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Email })
                .IsUnique()
                .HasFilter("\"Email\" <> ''");
            entity.HasIndex(x => new { x.TenantId, x.MobilePhone })
                .IsUnique()
                .HasFilter("\"MobilePhone\" <> ''");
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.MobilePhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SchoolType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.WorkShift).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Stages).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ContractType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PrimaryAmount).HasPrecision(18, 2);
            entity.Property(x => x.PrepAmount).HasPrecision(18, 2);
            entity.Property(x => x.SecondaryAmount).HasPrecision(18, 2);
            entity.Property(x => x.MonthlySalary).HasPrecision(18, 2);
            entity.Property(x => x.ZoomAccessToken).HasMaxLength(2000).IsRequired();
            entity.HasMany(x => x.CourseRates)
                .WithOne(x => x.Teacher)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(x => x.ZoomRefreshToken).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ZoomConnectedEmail).HasMaxLength(160).IsRequired();
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Avatar)
                .WithMany()
                .HasForeignKey(x => x.AvatarId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Avatar>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(60).IsRequired();
            entity.Property(x => x.AccentColor).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Emoji).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
            entity.HasMany(x => x.Grades)
                .WithOne(x => x.Stage)
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.StageId);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Term).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.SchoolType).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.Stage)
                .WithMany()
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.StageId);
            entity.HasMany(x => x.Units)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Lessons)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Quizzes)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseUnit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.CourseId, x.SortOrder });
            entity.HasMany(x => x.Lessons)
                .WithOne(x => x.Unit)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.UnitId);
            entity.HasMany(x => x.Steps)
                .WithOne(x => x.Lesson)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Videos)
                .WithOne(x => x.Lesson)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Prompt).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ExpectedAnswer).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<StudentProgress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.StepId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Progress)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(400).IsRequired();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasMany(x => x.Questions)
                .WithOne(x => x.Quiz)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Classroom)
                .WithMany()
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).HasMaxLength(300).IsRequired();
            entity.Property(x => x.OptionA).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OptionB).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OptionC).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OptionsJson).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.CorrectOption).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.User)
                .WithMany(x => x.QuizAttempts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Quiz)
                .WithMany()
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Answers)
                .WithOne(x => x.Attempt)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttemptAnswer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SelectedOption).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.BadgeId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Badges)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Badge)
                .WithMany()
                .HasForeignKey(x => x.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LiveSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ZoomMeetingId).HasMaxLength(40).IsRequired();
            entity.Property(x => x.JoinUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.StartUrl).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => x.StartsAtUtc);
            entity.HasOne(x => x.Host)
                .WithMany()
                .HasForeignKey(x => x.HostUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Classroom)
                .WithMany()
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.StartsAtUtc);
            entity.HasIndex(x => new { x.TeacherId, x.StartsAtUtc });
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FixedTimetableEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Period).HasConversion<string>().HasMaxLength(10);
            entity.Property(x => x.CombinedGrades).HasMaxLength(80);
            entity.HasIndex(x => new { x.TeacherId, x.DayOfWeek, x.Period, x.SessionNumber }).IsUnique();
            entity.HasIndex(x => new { x.DayOfWeek, x.Period, x.SessionNumber });
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeacherSessionAttendance>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TeacherId, x.CourseId, x.SessionDate }).IsUnique();
            entity.HasIndex(x => x.SessionDate);
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudentWeeklyReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InteractionDuringSession).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.TeacherId, x.StudentId, x.WeekStartDate }).IsUnique();
            entity.HasIndex(x => x.WeekStartDate);
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeeklyStudyPlan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.TeacherId, x.CourseId, x.FromDate }).IsUnique();
            entity.HasIndex(x => x.FromDate);
            entity.HasIndex(x => x.ToDate);
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items)
                .WithOne(x => x.Plan)
                .HasForeignKey(x => x.WeeklyStudyPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyStudyPlanItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WeeklyStudyPlanId, x.WeekNumber }).IsUnique();
            entity.HasMany(x => x.Topics)
                .WithOne(x => x.Week)
                .HasForeignKey(x => x.WeeklyStudyPlanItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyStudyPlanTopic>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.WeeklyStudyPlanItemId, x.SortOrder });
        });

        modelBuilder.Entity<TuitionPayment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.PaymentDate);
            entity.HasIndex(x => new { x.Year, x.Month });
            entity.HasIndex(x => x.ParentId);
            entity.HasIndex(x => x.StudentId);
            entity.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OtherExpense>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.ExpenseDate);
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<TeacherPayrollAdjustment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.AdjustmentDate);
            entity.HasIndex(x => x.TeacherId);
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeacherCourseRate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SessionAmount).HasPrecision(18, 2);
            entity.Property(x => x.MonthlySalary).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.TeacherId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.WhatsAppGroupInviteUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.WhatsAppNotifyPhones).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Courses)
                .WithOne(x => x.Classroom)
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Students)
                .WithOne(x => x.Classroom)
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassroomCourse>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ClassroomId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassroomStudent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ClassroomId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentCourseEnrollment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StudentId, x.ClassroomId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Classroom)
                .WithMany(x => x.CourseEnrollments)
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(800).IsRequired();
            entity.HasOne(x => x.Classroom)
                .WithMany()
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SolutionVideo)
                .WithMany()
                .HasForeignKey(x => x.SolutionVideoMediaAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Questions)
                .WithOne(x => x.Assignment)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Submissions)
                .WithOne(x => x.Assignment)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).HasMaxLength(500).IsRequired();
            entity.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.OptionA).HasMaxLength(120);
            entity.Property(x => x.OptionB).HasMaxLength(120);
            entity.Property(x => x.OptionC).HasMaxLength(120);
            entity.Property(x => x.CorrectAnswer).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AssignmentSubmission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.TeacherFeedback).HasMaxLength(800);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Answers)
                .WithOne(x => x.Submission)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentAnswer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AnswerText).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Prompt).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.PassageText).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.OptionA).HasMaxLength(200);
            entity.Property(x => x.OptionB).HasMaxLength(200);
            entity.Property(x => x.OptionC).HasMaxLength(200);
            entity.Property(x => x.OptionD).HasMaxLength(200);
            entity.Property(x => x.OptionsJson).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.CorrectAnswer).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(800).IsRequired();
            entity.HasOne(x => x.Classroom)
                .WithMany()
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Questions)
                .WithOne(x => x.Exam)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Attempts)
                .WithOne(x => x.Exam)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Prompt).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.PassageText).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.OptionA).HasMaxLength(200);
            entity.Property(x => x.OptionB).HasMaxLength(200);
            entity.Property(x => x.OptionC).HasMaxLength(200);
            entity.Property(x => x.OptionD).HasMaxLength(200);
            entity.Property(x => x.OptionsJson).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.CorrectAnswer).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.BankQuestion)
                .WithMany()
                .HasForeignKey(x => x.BankQuestionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentExamQuestionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.TeacherFeedback).HasMaxLength(800);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Answers)
                .WithOne(x => x.Attempt)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamAnswer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AnswerText).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.ExamQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageKey).HasMaxLength(400).IsRequired();
            entity.Property(x => x.ExternalUrl).HasMaxLength(1000);
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.UploadedBy)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonVideo>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.Videos)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.MediaAsset)
                .WithMany()
                .HasForeignKey(x => x.MediaAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VideoWatchSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MediaAssetId, x.StudentId });
            entity.HasOne(x => x.MediaAsset)
                .WithMany()
                .HasForeignKey(x => x.MediaAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WhatsAppReportLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReportType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.RecipientPhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.MessagePreview).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<SiteSettings>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SiteName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LogoStorageKey).HasMaxLength(400).IsRequired();
            entity.Property(x => x.LogoContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.BannerStorageKey).HasMaxLength(400).IsRequired();
            entity.Property(x => x.BannerContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AmSessionCount).HasDefaultValue(CodeKids.Domain.Entities.SiteSettings.DefaultSessionCount);
            entity.Property(x => x.PmSessionCount).HasDefaultValue(CodeKids.Domain.Entities.SiteSettings.DefaultSessionCount);
        });

        modelBuilder.Entity<TenantSignup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TenantSlug).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.MobilePhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.TenantSlug).IsUnique();
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        ApplyTenantFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenantOnInsert();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenantOnInsert();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTenantOnInsert()
    {
        if (string.IsNullOrWhiteSpace(CurrentTenantId))
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added
                && entry.Entity.TenantId is null
                && entry.Entity is not TenantSignup)
            {
                entry.Entity.TenantId = CurrentTenantId;
            }
        }
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(TenantEntity).IsAssignableFrom(entityType.ClrType)
                || entityType.ClrType.IsAbstract
                || entityType.ClrType == typeof(TenantSignup))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property(nameof(TenantEntity.TenantId)).HasMaxLength(64);
            builder.HasIndex(nameof(TenantEntity.TenantId));

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(TenantEntity.TenantId));
            var current = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
            var isShared = Expression.Equal(property, Expression.Constant(null, typeof(string)));
            var isCurrent = Expression.Equal(property, current);
            var body = Expression.OrElse(isShared, isCurrent);
            builder.HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
