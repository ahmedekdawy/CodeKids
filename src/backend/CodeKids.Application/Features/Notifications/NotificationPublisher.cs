using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Notifications;

public static class NotificationRecipients
{
    public static async Task<IReadOnlyList<Guid>> StudentsForAssignmentAsync(
        IAppDbContext dbContext,
        Guid classroomId,
        CancellationToken cancellationToken) =>
        await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.ClassroomId == classroomId)
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public static async Task<IReadOnlyList<Guid>> StudentsForExamAsync(
        IAppDbContext dbContext,
        Guid classroomId,
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        var studentIds = await StudentsForAssignmentAsync(dbContext, classroomId, cancellationToken);
        if (courseId is null || studentIds.Count == 0)
        {
            return studentIds;
        }

        var eligible = new List<Guid>();
        foreach (var studentId in studentIds)
        {
            var visible = await StudentCourseVisibility.GetVisibleCourseIdsAsync(dbContext, studentId, cancellationToken);
            if (visible.Contains(courseId.Value))
            {
                eligible.Add(studentId);
            }
        }

        return eligible;
    }

    public static async Task<IReadOnlyList<Guid>> StudentsForQuizAsync(
        IAppDbContext dbContext,
        Guid courseId,
        Guid? classroomId,
        CancellationToken cancellationToken)
    {
        if (classroomId is Guid cid)
        {
            var studentIds = await StudentsForAssignmentAsync(dbContext, cid, cancellationToken);
            var eligible = new List<Guid>();
            foreach (var studentId in studentIds)
            {
                var visible = await StudentCourseVisibility.GetVisibleCourseIdsAsync(dbContext, studentId, cancellationToken);
                if (visible.Contains(courseId))
                {
                    eligible.Add(studentId);
                }
            }

            return eligible;
        }

        var classroomIds = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .Select(x => x.ClassroomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (classroomIds.Count == 0)
        {
            return [];
        }

        var students = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.ClassroomId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var filtered = new List<Guid>();
        foreach (var studentId in students)
        {
            var visible = await StudentCourseVisibility.GetVisibleCourseIdsAsync(dbContext, studentId, cancellationToken);
            if (visible.Contains(courseId))
            {
                filtered.Add(studentId);
            }
        }

        return filtered;
    }

    public static async Task<Guid?> ParentIdForStudentAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => x.ParentId)
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class NotificationPublisher(IAppDbContext dbContext, INotificationRealtime? realtime = null)
{
    public Task NotifyAssignmentCreatedAsync(
        Assignment assignment,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.AssignmentCreated,
            assignment.Title,
            $"New assignment: {assignment.Title}",
            $"/assignments/{assignment.Id}",
            assignment.Id,
            relatedStudentId: null,
            () => NotificationRecipients.StudentsForAssignmentAsync(dbContext, assignment.ClassroomId, cancellationToken),
            cancellationToken);

    public Task NotifyExamCreatedAsync(
        Exam exam,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.ExamCreated,
            exam.Title,
            $"New exam: {exam.Title}",
            $"/exams/{exam.Id}",
            exam.Id,
            relatedStudentId: null,
            () => NotificationRecipients.StudentsForExamAsync(dbContext, exam.ClassroomId, exam.CourseId, cancellationToken),
            cancellationToken);

    public Task NotifyQuizCreatedAsync(
        Quiz quiz,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.QuizCreated,
            quiz.Title,
            $"New quiz: {quiz.Title}",
            $"/quizzes/{quiz.Id}",
            quiz.Id,
            relatedStudentId: null,
            () => NotificationRecipients.StudentsForQuizAsync(dbContext, quiz.CourseId, quiz.ClassroomId, cancellationToken),
            cancellationToken);

    public async Task NotifyAssignmentGradedAsync(
        AssignmentSubmission submission,
        CancellationToken cancellationToken)
    {
        var assignment = submission.Assignment
            ?? await dbContext.Assignments.AsNoTracking().FirstAsync(x => x.Id == submission.AssignmentId, cancellationToken);

        var student = await dbContext.Users.AsNoTracking()
            .FirstAsync(x => x.Id == submission.StudentId, cancellationToken);

        await NotifyUserAsync(
            submission.StudentId,
            NotificationKind.AssignmentGraded,
            assignment.Title,
            $"Your assignment \"{assignment.Title}\" was graded",
            $"/assignments/{assignment.Id}",
            assignment.Id,
            relatedStudentId: null,
            cancellationToken);

        if (student.ParentId is Guid parentId)
        {
            await NotifyUserAsync(
                parentId,
                NotificationKind.AssignmentGraded,
                assignment.Title,
                $"{student.DisplayName}: assignment \"{assignment.Title}\" was graded",
                $"/parent?child={submission.StudentId}",
                assignment.Id,
                submission.StudentId,
                cancellationToken);
        }
    }

    public async Task NotifyExamGradedAsync(
        ExamAttempt attempt,
        CancellationToken cancellationToken)
    {
        var exam = attempt.Exam
            ?? await dbContext.Exams.AsNoTracking().FirstAsync(x => x.Id == attempt.ExamId, cancellationToken);

        var student = await dbContext.Users.AsNoTracking()
            .FirstAsync(x => x.Id == attempt.StudentId, cancellationToken);

        await NotifyUserAsync(
            attempt.StudentId,
            NotificationKind.ExamGraded,
            exam.Title,
            $"Your exam \"{exam.Title}\" was graded",
            $"/exams/{exam.Id}",
            exam.Id,
            relatedStudentId: null,
            cancellationToken);

        if (student.ParentId is Guid parentId)
        {
            await NotifyUserAsync(
                parentId,
                NotificationKind.ExamGraded,
                exam.Title,
                $"{student.DisplayName}: exam \"{exam.Title}\" was graded",
                $"/parent?child={attempt.StudentId}",
                exam.Id,
                attempt.StudentId,
                cancellationToken);
        }
    }

    private async Task NotifyStudentsAsync(
        NotificationKind kind,
        string title,
        string body,
        string targetUrl,
        Guid entityId,
        Guid? relatedStudentId,
        Func<Task<IReadOnlyList<Guid>>> resolveStudents,
        CancellationToken cancellationToken)
    {
        var studentIds = await resolveStudents();
        foreach (var studentId in studentIds.Distinct())
        {
            await NotifyUserAsync(
                studentId,
                kind,
                title,
                body,
                targetUrl,
                entityId,
                relatedStudentId,
                cancellationToken);
        }
    }

    private async Task NotifyUserAsync(
        Guid userId,
        NotificationKind kind,
        string title,
        string body,
        string targetUrl,
        Guid entityId,
        Guid? relatedStudentId,
        CancellationToken cancellationToken)
    {
        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Title = title.Trim(),
            Body = body.Trim(),
            TargetUrl = targetUrl,
            EntityId = entityId,
            RelatedStudentId = relatedStudentId,
            IsRead = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (realtime is not null)
        {
            await realtime.PushAsync(userId, Map(notification), cancellationToken);
        }
    }

    internal static NotificationDto Map(UserNotification notification) =>
        new(
            notification.Id,
            notification.Kind.ToString(),
            notification.Title,
            notification.Body,
            notification.TargetUrl,
            notification.EntityId,
            notification.RelatedStudentId,
            notification.IsRead,
            notification.CreatedAtUtc);
}
