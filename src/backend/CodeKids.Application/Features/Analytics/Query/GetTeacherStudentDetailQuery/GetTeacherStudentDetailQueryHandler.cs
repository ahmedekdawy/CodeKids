using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

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
