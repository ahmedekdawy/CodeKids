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
        // Prefer students explicitly enrolled in this course (matches "enrolled on this course").
        var enrollmentQuery = dbContext.StudentCourseEnrollments
            .AsNoTracking()
            .Where(x => x.CourseId == courseId);
        if (classroomId is Guid scopedClassroomId)
        {
            enrollmentQuery = enrollmentQuery.Where(x => x.ClassroomId == scopedClassroomId);
        }

        var enrolled = await enrollmentQuery
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (enrolled.Count > 0)
        {
            return enrolled;
        }

        // No course enrollments yet — notify classroom members for the quiz scope.
        if (classroomId is Guid cid)
        {
            return await StudentsForAssignmentAsync(dbContext, cid, cancellationToken);
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

        return await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => classroomIds.Contains(x.ClassroomId))
            .Select(x => x.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
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

    public static async Task<IReadOnlyList<Guid>> TeachersForAssessmentAsync(
        IAppDbContext dbContext,
        Guid? classroomId,
        Guid? courseId,
        Guid? createdByUserId,
        CancellationToken cancellationToken)
    {
        var teacherIds = new HashSet<Guid>();
        if (createdByUserId is Guid creatorId && creatorId != Guid.Empty)
        {
            teacherIds.Add(creatorId);
        }

        if (classroomId is not null || courseId is not null)
        {
            var query = dbContext.ClassroomCourses.AsNoTracking().AsQueryable();
            if (classroomId is Guid cid)
            {
                query = query.Where(x => x.ClassroomId == cid);
            }

            if (courseId is Guid course)
            {
                query = query.Where(x => x.CourseId == course);
            }

            foreach (var id in await query.Select(x => x.TeacherId).Distinct().ToListAsync(cancellationToken))
            {
                teacherIds.Add(id);
            }
        }

        return teacherIds.ToList();
    }

    public static string TeacherTargetPath(string kind) => kind switch
    {
        "Assignment" => "/teacher/assignments",
        "Quiz" => "/teacher/quizzes",
        "Exam" => "/teacher/exams",
        _ => "/teacher"
    };
}

public sealed class NotificationPublisher(
    IAppDbContext dbContext,
    IAssessmentPublishNotificationQueue whatsAppQueue,
    INotificationRealtime? realtime = null)
{
    public Task NotifyAssignmentCreatedAsync(
        Assignment assignment,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.AssignmentCreated,
            $"New assignment: {assignment.Title}",
            new AssessmentPublishInfo(
                "Assignment",
                "واجب",
                assignment.Id,
                assignment.Title,
                $"/assignments/{assignment.Id}",
                assignment.DueAtUtc,
                assignment.ClassroomId,
                CourseId: null,
                assignment.CreatedByUserId),
            () => NotificationRecipients.StudentsForAssignmentAsync(dbContext, assignment.ClassroomId, cancellationToken),
            cancellationToken);

    public Task NotifyExamCreatedAsync(
        Exam exam,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.ExamCreated,
            $"New exam: {exam.Title}",
            new AssessmentPublishInfo(
                "Exam",
                "امتحان",
                exam.Id,
                exam.Title,
                $"/exams/{exam.Id}",
                exam.DueAtUtc,
                exam.ClassroomId,
                exam.CourseId,
                exam.CreatedByUserId),
            () => NotificationRecipients.StudentsForExamAsync(dbContext, exam.ClassroomId, exam.CourseId, cancellationToken),
            cancellationToken);

    public Task NotifyQuizCreatedAsync(
        Quiz quiz,
        CancellationToken cancellationToken) =>
        NotifyStudentsAsync(
            NotificationKind.QuizCreated,
            $"New quiz: {quiz.Title}",
            new AssessmentPublishInfo(
                "Quiz",
                "كويز",
                quiz.Id,
                quiz.Title,
                $"/quizzes/{quiz.Id}",
                DueAtUtc: null,
                quiz.ClassroomId,
                quiz.CourseId,
                quiz.CreatedByUserId),
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
        string body,
        AssessmentPublishInfo publishInfo,
        Func<Task<IReadOnlyList<Guid>>> resolveStudents,
        CancellationToken cancellationToken)
    {
        var studentIds = (await resolveStudents()).Distinct().ToList();

        // Queue WhatsApp first so a slow/failing in-app fan-out cannot skip messaging.
        whatsAppQueue.Enqueue(dbContext.CurrentTenantId, publishInfo, studentIds);

        foreach (var studentId in studentIds)
        {
            await NotifyUserAsync(
                studentId,
                kind,
                publishInfo.Title,
                body,
                publishInfo.TargetPath,
                publishInfo.EntityId,
                relatedStudentId: null,
                cancellationToken);
        }

        var teacherIds = await NotificationRecipients.TeachersForAssessmentAsync(
            dbContext,
            publishInfo.ClassroomId,
            publishInfo.CourseId,
            publishInfo.CreatedByUserId,
            cancellationToken);
        var teacherPath = NotificationRecipients.TeacherTargetPath(publishInfo.Kind);
        foreach (var teacherId in teacherIds)
        {
            await NotifyUserAsync(
                teacherId,
                kind,
                publishInfo.Title,
                body,
                teacherPath,
                publishInfo.EntityId,
                relatedStudentId: null,
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
