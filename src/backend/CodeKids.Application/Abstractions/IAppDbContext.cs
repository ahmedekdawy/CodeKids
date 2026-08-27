using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Avatar> Avatars { get; }
    DbSet<Course> Courses { get; }
    DbSet<Stage> Stages { get; }
    DbSet<Grade> Grades { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<SubjectUnit> SubjectUnits { get; }
    DbSet<SubjectUnitLesson> SubjectUnitLessons { get; }
    DbSet<LessonStep> LessonSteps { get; }
    DbSet<StudentProgress> StudentProgress { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<LiveSession> LiveSessions { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<FixedTimetableEntry> FixedTimetableEntries { get; }
    DbSet<TeacherSessionAttendance> TeacherSessionAttendances { get; }
    DbSet<StudentWeeklyReport> StudentWeeklyReports { get; }
    DbSet<WeeklyStudyPlan> WeeklyStudyPlans { get; }
    DbSet<WeeklyStudyPlanItem> WeeklyStudyPlanItems { get; }
    DbSet<WeeklyStudyPlanTopic> WeeklyStudyPlanTopics { get; }
    DbSet<TuitionPayment> TuitionPayments { get; }
    DbSet<OtherExpense> OtherExpenses { get; }
    DbSet<TeacherPayrollAdjustment> TeacherPayrollAdjustments { get; }
    DbSet<TeacherCourseRate> TeacherCourseRates { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<Classroom> Classrooms { get; }
    DbSet<ClassroomCourse> ClassroomCourses { get; }
    DbSet<ClassroomStudent> ClassroomStudents { get; }
    DbSet<StudentCourseEnrollment> StudentCourseEnrollments { get; }
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
    DbSet<SiteSettings> SiteSettings { get; }
    DbSet<TenantSignup> TenantSignups { get; }

    string? CurrentTenantId { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
