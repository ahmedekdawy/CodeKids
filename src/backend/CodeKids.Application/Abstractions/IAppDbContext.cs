using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Avatar> Avatars { get; }
    DbSet<Course> Courses { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonStep> LessonSteps { get; }
    DbSet<StudentProgress> StudentProgress { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<LiveSession> LiveSessions { get; }
    DbSet<Classroom> Classrooms { get; }
    DbSet<ClassroomStudent> ClassroomStudents { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<AssignmentQuestion> AssignmentQuestions { get; }
    DbSet<AssignmentSubmission> AssignmentSubmissions { get; }
    DbSet<AssignmentAnswer> AssignmentAnswers { get; }
    DbSet<BankQuestion> BankQuestions { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamQuestion> ExamQuestions { get; }
    DbSet<ExamAttempt> ExamAttempts { get; }
    DbSet<ExamAnswer> ExamAnswers { get; }
    DbSet<MediaAsset> MediaAssets { get; }
    DbSet<LessonVideo> LessonVideos { get; }
    DbSet<VideoWatchSession> VideoWatchSessions { get; }
    DbSet<WhatsAppReportLog> WhatsAppReportLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
