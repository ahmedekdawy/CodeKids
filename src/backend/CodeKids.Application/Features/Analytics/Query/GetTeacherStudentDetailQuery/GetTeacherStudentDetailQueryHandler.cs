using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
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

        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(x => classroomCourseIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var outlines = await CourseOutlineResolver.ResolveManyAsync(dbContext, courses, cancellationToken);
        var catalogLessons = outlines.Values.SelectMany(o => o.Lessons).ToList();
        var lessonIds = catalogLessons.Select(l => l.Id).ToList();
        var stepCounts = await dbContext.LessonSteps
            .AsNoTracking()
            .Where(x => lessonIds.Contains(x.LessonId))
            .GroupBy(x => x.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, cancellationToken);
        var videoDurations = await dbContext.LessonVideos
            .AsNoTracking()
            .Where(x => lessonIds.Contains(x.LessonId))
            .Select(x => new { x.LessonId, x.MediaAsset!.DurationSeconds })
            .ToListAsync(cancellationToken);
        var durationByLesson = videoDurations
            .GroupBy(x => x.LessonId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.DurationSeconds).Where(d => d is > 0).Cast<int>().DefaultIfEmpty(0).Max());

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

        var mastery = catalogLessons.Select(lesson =>
        {
            var totalSteps = Math.Max(1, stepCounts.GetValueOrDefault(lesson.Id));
            var completed = completedByLesson.GetValueOrDefault(lesson.Id);
            var duration = durationByLesson.GetValueOrDefault(lesson.Id);
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
                stepCounts.GetValueOrDefault(lesson.Id),
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

        var titleByLesson = catalogLessons.ToDictionary(x => x.Id, x => x.Title);
        var recentWatch = await dbContext.VideoWatchSessions
            .AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.LastEventAtUtc)
            .Take(8)
            .Select(x => new
            {
                x.MediaAssetId,
                x.LessonId,
                x.ActualWatchSeconds,
                x.UsedSpeedUp,
                x.SkippedAhead,
                x.LastEventAtUtc
            })
            .ToListAsync(cancellationToken);
        var recentWatchDtos = recentWatch.Select(x => new WatchSummaryDto(
            x.MediaAssetId,
            x.LessonId,
            x.LessonId is Guid lid && titleByLesson.TryGetValue(lid, out var title) ? title : null,
            x.ActualWatchSeconds,
            x.UsedSpeedUp,
            x.SkippedAhead,
            x.LastEventAtUtc)).ToList();

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
            recentWatchDtos);
    }
}
