using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Dashboard;

public sealed class GetParentChildOverviewQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetParentChildOverviewQuery, ParentChildOverviewDto>
{
    public async Task<ParentChildOverviewDto> Handle(
        GetParentChildOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var child = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ChildId && x.Role == UserRole.Student, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        if (child.ParentId != query.ParentId)
        {
            throw new InvalidOperationException("This student is not linked to your account.");
        }

        var grades = await StudentGradeResolver.ResolveAsync(
            dbContext,
            [(child.Id, child.Grade)],
            cancellationToken);
        var grade = grades.GetValueOrDefault(child.Id) ?? child.Grade;

        var visibleCourseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
            dbContext, child.Id, cancellationToken);

        var evaluations = await dbContext.StudentWeeklyReports
            .AsNoTracking()
            .Where(x => x.StudentId == child.Id)
            .OrderByDescending(x => x.WeekStartDate)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Take(20)
            .Select(x => new ChildEvaluationSummaryDto(
                x.WeekStartDate,
                x.Teacher != null ? x.Teacher.DisplayName : null,
                x.PerformancePercent,
                x.AttendancePercent,
                x.HomeworkPercent,
                x.InteractionDuringSession,
                x.OpenCamera))
            .ToListAsync(cancellationToken);

        if (visibleCourseIds.Count == 0)
        {
            return new ParentChildOverviewDto(child.Id, child.DisplayName, grade, evaluations, []);
        }

        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(c => visibleCourseIds.Contains(c.Id))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Theme,
                c.Description,
                c.Grade,
                c.Term
            })
            .ToListAsync(cancellationToken);

        var classroomIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.StudentId == child.Id)
            .Select(x => x.ClassroomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var assignedLinks = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.ClassroomId) && visibleCourseIds.Contains(x.CourseId))
            .Select(x => new { x.ClassroomId, x.CourseId })
            .ToListAsync(cancellationToken);

        var legacyLinks = await dbContext.Classrooms
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.Id) && x.CourseId != null && visibleCourseIds.Contains(x.CourseId.Value))
            .Select(x => new { ClassroomId = x.Id, CourseId = x.CourseId!.Value })
            .ToListAsync(cancellationToken);

        var classroomsByCourse = assignedLinks
            .Concat(legacyLinks)
            .GroupBy(x => x.CourseId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ClassroomId).ToHashSet());

        var relevantClassroomIds = classroomsByCourse.Values.SelectMany(x => x).Distinct().ToList();

        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => relevantClassroomIds.Contains(a.ClassroomId))
            .Select(a => new
            {
                a.Id,
                a.ClassroomId,
                a.Title,
                a.Description,
                a.DueAtUtc,
                a.CreatedAtUtc,
                MaxScore = (int?)a.Questions.Sum(q => (int?)q.Points)
            })
            .ToListAsync(cancellationToken);

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var submissions = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(s => s.StudentId == child.Id && assignmentIds.Contains(s.AssignmentId))
            .Select(s => new
            {
                s.AssignmentId,
                s.Status,
                s.Score,
                s.MaxScore,
                s.TeacherFeedback,
                s.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);

        var latestSubmissionByAssignment = submissions
            .GroupBy(s => s.AssignmentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.SubmittedAtUtc).First());

        var exams = await dbContext.Exams
            .AsNoTracking()
            .Where(e => relevantClassroomIds.Contains(e.ClassroomId)
                && (e.CourseId == null || visibleCourseIds.Contains(e.CourseId.Value)))
            .Select(e => new
            {
                e.Id,
                e.ClassroomId,
                e.CourseId,
                e.Title,
                e.Description,
                e.DueAtUtc,
                e.CreatedAtUtc,
                MaxScore = (int?)e.Questions.Sum(q => (int?)q.Points)
            })
            .ToListAsync(cancellationToken);

        var examIds = exams.Select(e => e.Id).ToList();
        var examAttempts = await dbContext.ExamAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == child.Id && examIds.Contains(a.ExamId))
            .Select(a => new
            {
                a.ExamId,
                a.Status,
                a.Score,
                a.MaxScore,
                a.TeacherFeedback,
                a.StartedAtUtc,
                a.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);

        var latestAttemptByExam = examAttempts
            .GroupBy(a => a.ExamId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SubmittedAtUtc ?? x.StartedAtUtc).First());

        var quizzes = await dbContext.Quizzes
            .AsNoTracking()
            .Where(q => visibleCourseIds.Contains(q.CourseId)
                && (q.ClassroomId == null || classroomIds.Contains(q.ClassroomId.Value)))
            .Select(q => new
            {
                q.Id,
                q.CourseId,
                q.Title,
                q.Description,
                q.XpReward,
                TotalQuestions = q.Questions.Count,
                q.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var quizIds = quizzes.Select(q => q.Id).ToList();
        var quizAttempts = await dbContext.QuizAttempts
            .AsNoTracking()
            .Where(a => a.UserId == child.Id && quizIds.Contains(a.QuizId))
            .Select(a => new
            {
                a.QuizId,
                a.Score,
                a.TotalQuestions,
                a.EarnedXp,
                a.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        var latestQuizAttempt = quizAttempts
            .GroupBy(a => a.QuizId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAtUtc).First());

        var courseDtos = courses.Select(course =>
        {
            var rooms = classroomsByCourse.GetValueOrDefault(course.Id) ?? [];

            var courseAssignments = assignments
                .Where(a => rooms.Contains(a.ClassroomId))
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a =>
                {
                    latestSubmissionByAssignment.TryGetValue(a.Id, out var submission);
                    return new ParentAssessmentItemDto(
                        a.Id,
                        a.Title,
                        a.Description,
                        a.DueAtUtc,
                        submission?.Status.ToString() ?? "NotStarted",
                        submission?.Score,
                        submission?.MaxScore ?? a.MaxScore,
                        submission?.TeacherFeedback,
                        submission?.SubmittedAtUtc);
                })
                .ToList();

            var courseExams = exams
                .Where(e => e.CourseId == course.Id || (e.CourseId == null && rooms.Contains(e.ClassroomId)))
                .OrderByDescending(e => e.CreatedAtUtc)
                .Select(e =>
                {
                    latestAttemptByExam.TryGetValue(e.Id, out var attempt);
                    return new ParentAssessmentItemDto(
                        e.Id,
                        e.Title,
                        e.Description,
                        e.DueAtUtc,
                        attempt?.Status.ToString() ?? "NotStarted",
                        attempt?.Score,
                        attempt?.MaxScore ?? e.MaxScore,
                        attempt?.TeacherFeedback,
                        attempt?.SubmittedAtUtc ?? attempt?.StartedAtUtc);
                })
                .ToList();

            var courseQuizzes = quizzes
                .Where(q => q.CourseId == course.Id)
                .OrderByDescending(q => q.CreatedAtUtc)
                .Select(q =>
                {
                    latestQuizAttempt.TryGetValue(q.Id, out var attempt);
                    return new ParentQuizItemDto(
                        q.Id,
                        q.Title,
                        q.Description,
                        q.XpReward,
                        attempt?.TotalQuestions ?? q.TotalQuestions,
                        attempt?.Score,
                        attempt?.EarnedXp,
                        attempt?.CompletedAtUtc);
                })
                .ToList();

            return new ParentChildCourseDto(
                course.Id,
                course.Title,
                course.Theme,
                course.Description,
                course.Grade,
                course.Term?.ToString(),
                courseAssignments,
                courseExams,
                courseQuizzes);
        }).ToList();

        return new ParentChildOverviewDto(
            child.Id,
            child.DisplayName,
            grade,
            evaluations,
            courseDtos);
    }
}
