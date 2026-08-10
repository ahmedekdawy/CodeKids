using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public static class StudentLevelCalculator
{
    private static readonly (int MinXp, string Code, string Name)[] Levels =
    [
        (0, "L1", "Beginner"),
        (100, "L2", "Explorer"),
        (250, "L3", "Coder"),
        (500, "L4", "Pro"),
        (1000, "L5", "Master")
    ];

    public static (int LevelNumber, string Code, string Name, int MinXp, int? NextMinXp, int ProgressPercent) FromXp(int totalXp)
    {
        var xp = Math.Max(0, totalXp);
        var index = 0;
        for (var i = 0; i < Levels.Length; i++)
        {
            if (xp >= Levels[i].MinXp) index = i;
        }

        var current = Levels[index];
        int? nextMin = index + 1 < Levels.Length ? Levels[index + 1].MinXp : null;
        var progress = nextMin is int next
            ? (int)Math.Clamp(Math.Round((xp - current.MinXp) * 100.0 / Math.Max(1, next - current.MinXp)), 0, 100)
            : 100;

        return (index + 1, current.Code, current.Name, current.MinXp, nextMin, progress);
    }
}

public sealed record LessonWeaknessDto(
    Guid LessonId,
    string LessonTitle,
    int WrongAnswers,
    int TotalAnswers,
    int AccuracyPercent);

public sealed record LessonMasteryDto(
    Guid LessonId,
    string LessonTitle,
    int CompletedSteps,
    int TotalSteps,
    int ActualWatchSeconds,
    int? VideoDurationSeconds,
    int MasteryPercent);

public sealed record StudentLevelDto(
    int LevelNumber,
    string Code,
    string Name,
    int MinXp,
    int? NextMinXp,
    int ProgressPercent);

public sealed record TeacherStudentDetailDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    string? MobilePhone,
    string? ParentName,
    string? ParentMobilePhone,
    int TotalXp,
    StudentLevelDto Level,
    int CompletedSteps,
    int QuizAttempts,
    int ExamAttempts,
    int AssignmentSubmissions,
    IReadOnlyList<LessonMasteryDto> LessonMastery,
    IReadOnlyList<LessonWeaknessDto> WeakLessons,
    IReadOnlyList<WatchSummaryDto> RecentWatch);

public sealed record WatchSummaryDto(
    Guid MediaAssetId,
    Guid? LessonId,
    string? LessonTitle,
    int ActualWatchSeconds,
    bool UsedSpeedUp,
    bool SkippedAhead,
    DateTimeOffset LastEventAtUtc);

public sealed record GetTeacherStudentDetailQuery(Guid TeacherUserId, Guid StudentId)
    : IQuery<TeacherStudentDetailDto>;

public sealed record ClassroomDiagnosisDto(
    Guid ClassroomId,
    string ClassroomName,
    IReadOnlyList<LessonWeaknessDto> WeakLessons,
    IReadOnlyList<string> BehindStudents,
    IReadOnlyList<string> StrongStudents);

public sealed record GetClassroomDiagnosisQuery(Guid TeacherUserId, Guid ClassroomId)
    : IQuery<ClassroomDiagnosisDto>;

public sealed record RunDailyWhatsAppReportsCommand(bool Force = false) : ICommand<DailyWhatsAppReportsResultDto>;

public sealed record DailyWhatsAppReportsResultDto(
    int StudentMessagesAttempted,
    int ParentMessagesAttempted,
    int SentCount,
    int FailedCount,
    int SkippedCount);

public sealed class GetTeacherStudentDetailQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherStudentDetailQuery, TeacherStudentDetailDto>
{
    public async Task<TeacherStudentDetailDto> Handle(
        GetTeacherStudentDetailQuery query,
        CancellationToken cancellationToken)
    {
        var enrolled = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Include(x => x.Classroom)
            .AnyAsync(
                x => x.StudentId == query.StudentId && x.Classroom!.Courses.Any(t => t.TeacherId == query.TeacherUserId),
                cancellationToken);

        if (!enrolled)
        {
            throw new InvalidOperationException("Student is not in your classrooms.");
        }

        var student = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Parent)
            .FirstOrDefaultAsync(x => x.Id == query.StudentId && x.Role == UserRole.Student, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var classroomCourseIds = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => x.TeacherId == query.TeacherUserId)
            .Select(x => x.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (classroomCourseIds.Count == 0)
        {
            classroomCourseIds = await dbContext.Classrooms
                .AsNoTracking()
                .Where(x => x.Courses.Any(t => t.TeacherId == query.TeacherUserId) && x.CourseId != null)
                .Select(x => x.CourseId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        var lessons = await dbContext.Lessons
            .AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Videos)
                .ThenInclude(v => v.MediaAsset)
            .Where(x => classroomCourseIds.Contains(x.CourseId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var completedByLesson = await dbContext.StudentProgress
            .AsNoTracking()
            .Where(x => x.UserId == student.Id && x.IsCompleted)
            .GroupBy(x => x.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, cancellationToken);

        var watchByLesson = await dbContext.VideoWatchSessions
            .AsNoTracking()
            .Where(x => x.StudentId == student.Id && x.LessonId != null)
            .GroupBy(x => x.LessonId!.Value)
            .Select(g => new
            {
                LessonId = g.Key,
                Seconds = g.Sum(x => x.ActualWatchSeconds)
            })
            .ToDictionaryAsync(x => x.LessonId, x => x.Seconds, cancellationToken);

        var mastery = lessons.Select(lesson =>
        {
            var totalSteps = Math.Max(1, lesson.Steps.Count);
            var completed = completedByLesson.GetValueOrDefault(lesson.Id);
            var duration = lesson.Videos
                .Select(v => v.MediaAsset?.DurationSeconds)
                .Where(d => d is > 0)
                .Cast<int>()
                .DefaultIfEmpty(0)
                .Max();
            var watched = watchByLesson.GetValueOrDefault(lesson.Id);
            var stepPct = (int)Math.Round(completed * 100.0 / totalSteps);
            var watchPct = duration > 0
                ? (int)Math.Clamp(Math.Round(watched * 100.0 / duration), 0, 100)
                : stepPct;
            var masteryPct = duration > 0 ? (stepPct + watchPct) / 2 : stepPct;
            return new LessonMasteryDto(
                lesson.Id,
                lesson.Title,
                completed,
                lesson.Steps.Count,
                watched,
                duration > 0 ? duration : null,
                masteryPct);
        }).ToList();

        var weaknesses = await AnalyticsQueries.GetWeakLessonsForStudent(
            dbContext, student.Id, cancellationToken);

        var quizAttempts = await dbContext.QuizAttempts.CountAsync(x => x.UserId == student.Id, cancellationToken);
        var examAttempts = await dbContext.ExamAttempts.CountAsync(
            x => x.StudentId == student.Id && x.Status != ExamAttemptStatus.InProgress, cancellationToken);
        var assignmentSubs = await dbContext.AssignmentSubmissions.CountAsync(
            x => x.StudentId == student.Id, cancellationToken);
        var completedSteps = completedByLesson.Values.Sum();

        var recentWatch = await dbContext.VideoWatchSessions
            .AsNoTracking()
            .Include(x => x.Lesson)
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.LastEventAtUtc)
            .Take(8)
            .Select(x => new WatchSummaryDto(
                x.MediaAssetId,
                x.LessonId,
                x.Lesson != null ? x.Lesson.Title : null,
                x.ActualWatchSeconds,
                x.UsedSpeedUp,
                x.SkippedAhead,
                x.LastEventAtUtc))
            .ToListAsync(cancellationToken);

        var level = StudentLevelCalculator.FromXp(student.TotalXp);

        return new TeacherStudentDetailDto(
            student.Id,
            student.DisplayName,
            student.Email,
            string.IsNullOrWhiteSpace(student.MobilePhone) ? null : student.MobilePhone,
            student.Parent?.DisplayName,
            string.IsNullOrWhiteSpace(student.Parent?.MobilePhone) ? null : student.Parent!.MobilePhone,
            student.TotalXp,
            new StudentLevelDto(level.LevelNumber, level.Code, level.Name, level.MinXp, level.NextMinXp, level.ProgressPercent),
            completedSteps,
            quizAttempts,
            examAttempts,
            assignmentSubs,
            mastery,
            weaknesses,
            recentWatch);
    }
}

public sealed class GetClassroomDiagnosisQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomDiagnosisQuery, ClassroomDiagnosisDto>
{
    public async Task<ClassroomDiagnosisDto> Handle(
        GetClassroomDiagnosisQuery query,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Students)
            .FirstOrDefaultAsync(
                x => x.Id == query.ClassroomId && x.Courses.Any(t => t.TeacherId == query.TeacherUserId),
                cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var studentIds = classroom.Students.Select(x => x.StudentId).ToList();
        var students = await dbContext.Users
            .AsNoTracking()
            .Where(x => studentIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var weaknesses = await AnalyticsQueries.GetWeakLessonsForClassroom(
            dbContext, studentIds, cancellationToken);

        var avgXp = students.Count == 0 ? 0 : students.Average(x => x.TotalXp);
        var behind = students
            .Where(x => x.TotalXp < avgXp * 0.6 || StudentLevelCalculator.FromXp(x.TotalXp).LevelNumber <= 1)
            .OrderBy(x => x.TotalXp)
            .Select(x => x.DisplayName)
            .Take(8)
            .ToList();
        var strong = students
            .OrderByDescending(x => x.TotalXp)
            .Take(5)
            .Select(x => x.DisplayName)
            .ToList();

        return new ClassroomDiagnosisDto(
            classroom.Id,
            classroom.Name,
            weaknesses.Take(8).ToList(),
            behind,
            strong);
    }
}

public static class AnalyticsQueries
{
    public static async Task<IReadOnlyList<LessonWeaknessDto>> GetWeakLessonsForStudent(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ExamAnswers
            .AsNoTracking()
            .Include(x => x.Attempt)
            .Include(x => x.Question)
                .ThenInclude(q => q!.Lesson)
            .Where(x => x.Attempt!.StudentId == studentId
                        && x.Question!.LessonId != null
                        && x.IsCorrect != null)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => new { LessonId = x.Question!.LessonId!.Value, Title = x.Question.Lesson?.Title ?? "Lesson" })
            .Select(g =>
            {
                var total = g.Count();
                var wrong = g.Count(x => x.IsCorrect == false);
                var accuracy = total == 0 ? 100 : (int)Math.Round((total - wrong) * 100.0 / total);
                return new LessonWeaknessDto(g.Key.LessonId, g.Key.Title, wrong, total, accuracy);
            })
            .Where(x => x.WrongAnswers > 0)
            .OrderBy(x => x.AccuracyPercent)
            .ThenByDescending(x => x.WrongAnswers)
            .Take(10)
            .ToList();
    }

    public static async Task<IReadOnlyList<LessonWeaknessDto>> GetWeakLessonsForClassroom(
        IAppDbContext dbContext,
        IReadOnlyList<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.ExamAnswers
            .AsNoTracking()
            .Include(x => x.Attempt)
            .Include(x => x.Question)
                .ThenInclude(q => q!.Lesson)
            .Where(x => studentIds.Contains(x.Attempt!.StudentId)
                        && x.Question!.LessonId != null
                        && x.IsCorrect != null)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => new { LessonId = x.Question!.LessonId!.Value, Title = x.Question.Lesson?.Title ?? "Lesson" })
            .Select(g =>
            {
                var total = g.Count();
                var wrong = g.Count(x => x.IsCorrect == false);
                var accuracy = total == 0 ? 100 : (int)Math.Round((total - wrong) * 100.0 / total);
                return new LessonWeaknessDto(g.Key.LessonId, g.Key.Title, wrong, total, accuracy);
            })
            .Where(x => x.WrongAnswers > 0 && x.AccuracyPercent < 70)
            .OrderBy(x => x.AccuracyPercent)
            .Take(10)
            .ToList();
    }

    public static async Task<string> BuildStudentDigestAsync(
        IAppDbContext dbContext,
        User student,
        CancellationToken cancellationToken)
    {
        var level = StudentLevelCalculator.FromXp(student.TotalXp);
        var completedToday = await dbContext.StudentProgress.CountAsync(
            x => x.UserId == student.Id
                 && x.IsCompleted
                 && x.CompletedAtUtc.Date == DateTime.UtcNow.Date,
            cancellationToken);

        var watchToday = await dbContext.VideoWatchSessions
            .Where(x => x.StudentId == student.Id && x.LastEventAtUtc.Date == DateTime.UtcNow.Date)
            .SumAsync(x => (int?)x.ActualWatchSeconds, cancellationToken) ?? 0;

        var weaknesses = await GetWeakLessonsForStudent(dbContext, student.Id, cancellationToken);
        var weakText = weaknesses.Count == 0
            ? "No weak lessons flagged today."
            : "Focus: " + string.Join(", ", weaknesses.Take(2).Select(w => w.LessonTitle));

        return
            $"CodeKids daily report for {student.DisplayName}\n" +
            $"Level {level.LevelNumber} ({level.Name}) · {student.TotalXp} XP\n" +
            $"Today: {completedToday} steps · {watchToday}s video watched\n" +
            weakText;
    }
}

public sealed class RunDailyWhatsAppReportsCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppClient whatsAppClient)
    : ICommandHandler<RunDailyWhatsAppReportsCommand, DailyWhatsAppReportsResultDto>
{
    public async Task<DailyWhatsAppReportsResultDto> Handle(
        RunDailyWhatsAppReportsCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.Force)
        {
            var already = await dbContext.WhatsAppReportLogs.AnyAsync(
                x => x.ReportType == "DailyDigest"
                     && x.SentAtUtc.Date == DateTime.UtcNow.Date
                     && x.Status != "Skipped",
                cancellationToken);
            if (already)
            {
                return new DailyWhatsAppReportsResultDto(0, 0, 0, 0, 1);
            }
        }

        var classrooms = await dbContext.Classrooms
            .Include(x => x.Students)
            .Where(x => x.DailyWhatsAppReportsEnabled)
            .ToListAsync(cancellationToken);

        var studentIds = classrooms.SelectMany(c => c.Students.Select(s => s.StudentId)).Distinct().ToList();
        var students = await dbContext.Users
            .Include(x => x.Parent)
            .Where(x => studentIds.Contains(x.Id) && x.Role == UserRole.Student)
            .ToListAsync(cancellationToken);

        var attemptedStudent = 0;
        var attemptedParent = 0;
        var sent = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var student in students)
        {
            var message = await AnalyticsQueries.BuildStudentDigestAsync(dbContext, student, cancellationToken);
            var classroomId = classrooms
                .FirstOrDefault(c => c.Students.Any(s => s.StudentId == student.Id))?.Id;

            if (!string.IsNullOrWhiteSpace(student.MobilePhone))
            {
                attemptedStudent++;
                var result = await whatsAppClient.SendTextAsync(student.MobilePhone, message, cancellationToken);
                await LogAsync(dbContext, classroomId, student.Id, "DailyDigest", student.MobilePhone,
                    result.Sent ? "Sent" : "Failed", Truncate(result.Sent ? message : result.Detail), cancellationToken);
                if (result.Sent) sent++;
                else failed++;
            }
            else
            {
                skipped++;
            }

            var parentPhone = student.Parent?.MobilePhone?.Trim();
            if (!string.IsNullOrWhiteSpace(parentPhone))
            {
                attemptedParent++;
                var parentMessage =
                    $"Parent update — {student.DisplayName}\n" + message;
                var result = await whatsAppClient.SendTextAsync(parentPhone, parentMessage, cancellationToken);
                await LogAsync(dbContext, classroomId, student.Id, "DailyDigestParent", parentPhone,
                    result.Sent ? "Sent" : "Failed", Truncate(result.Sent ? parentMessage : result.Detail), cancellationToken);
                if (result.Sent) sent++;
                else failed++;
            }
        }

        if (students.Count == 0)
        {
            dbContext.WhatsAppReportLogs.Add(new WhatsAppReportLog
            {
                Id = Guid.NewGuid(),
                ReportType = "DailyDigest",
                RecipientPhone = "",
                Status = "Skipped",
                MessagePreview = "No enrolled students in classrooms with daily reports enabled.",
                SentAtUtc = DateTimeOffset.UtcNow
            });
            skipped++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DailyWhatsAppReportsResultDto(attemptedStudent, attemptedParent, sent, failed, skipped);
    }

    private static async Task LogAsync(
        IAppDbContext dbContext,
        Guid? classroomId,
        Guid? studentId,
        string reportType,
        string phone,
        string status,
        string preview,
        CancellationToken cancellationToken)
    {
        dbContext.WhatsAppReportLogs.Add(new WhatsAppReportLog
        {
            Id = Guid.NewGuid(),
            ClassroomId = classroomId,
            StudentId = studentId,
            ReportType = reportType,
            RecipientPhone = phone,
            Status = status,
            MessagePreview = preview,
            SentAtUtc = DateTimeOffset.UtcNow
        });
        await Task.CompletedTask;
    }

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..997] + "...";
}
