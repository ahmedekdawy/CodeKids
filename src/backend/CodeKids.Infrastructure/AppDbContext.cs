using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonStep> LessonSteps => Set<LessonStep>();
    public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<ClassroomStudent> ClassroomStudents => Set<ClassroomStudent>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentQuestion> AssignmentQuestions => Set<AssignmentQuestion>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<AssignmentAnswer> AssignmentAnswers => Set<AssignmentAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.MobilePhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ZoomAccessToken).HasMaxLength(2000).IsRequired();
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

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasMany(x => x.Lessons)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Quizzes)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasMany(x => x.Steps)
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
            entity.Property(x => x.OptionA).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OptionB).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OptionC).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CorrectOption).HasMaxLength(1).IsRequired();
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

        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.WhatsAppGroupInviteUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.WhatsAppNotifyPhones).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Students)
                .WithOne(x => x.Classroom)
                .HasForeignKey(x => x.ClassroomId)
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
    }
}
