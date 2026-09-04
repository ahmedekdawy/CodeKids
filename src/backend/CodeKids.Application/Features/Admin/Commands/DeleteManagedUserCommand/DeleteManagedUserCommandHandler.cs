using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class DeleteManagedUserCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteManagedUserCommand, bool>
{
    public async Task<bool> Handle(DeleteManagedUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.UserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role == UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last Super Admin.");
            }
        }

        var uid = command.UserId;
        var adminId = command.AdminUserId;

        foreach (var child in await dbContext.Users.Where(x => x.ParentId == uid).ToListAsync(cancellationToken))
        {
            child.ParentId = null;
        }

        foreach (var row in await dbContext.Quizzes.Where(x => x.CreatedByUserId == uid).ToListAsync(cancellationToken))
        {
            row.CreatedByUserId = null;
        }

        foreach (var row in await dbContext.StudentAskedQuestions.Where(x => x.TeacherId == uid).ToListAsync(cancellationToken))
        {
            row.TeacherId = null;
        }

        foreach (var row in await dbContext.TuitionPayments.Where(x => x.ParentId == uid || x.StudentId == uid).ToListAsync(cancellationToken))
        {
            if (row.ParentId == uid) row.ParentId = null;
            if (row.StudentId == uid) row.StudentId = null;
        }

        foreach (var row in await dbContext.UserNotifications.Where(x => x.RelatedStudentId == uid).ToListAsync(cancellationToken))
        {
            row.RelatedStudentId = null;
        }

        foreach (var row in await dbContext.ChatRoomMembers.Where(x => x.BlockedByUserId == uid).ToListAsync(cancellationToken))
        {
            row.BlockedByUserId = null;
        }

        foreach (var row in await dbContext.ChatMessages.Where(x => x.DeletedByUserId == uid).ToListAsync(cancellationToken))
        {
            row.DeletedByUserId = null;
        }

        foreach (var row in await dbContext.WhatsAppReportLogs.Where(x => x.StudentId == uid).ToListAsync(cancellationToken))
        {
            row.StudentId = null;
        }

        await ReassignAsync(dbContext.Assignments.Where(x => x.CreatedByUserId == uid), x => x.CreatedByUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.BankQuestions.Where(x => x.CreatedByUserId == uid), x => x.CreatedByUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.Exams.Where(x => x.CreatedByUserId == uid), x => x.CreatedByUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.ChatRooms.Where(x => x.CreatedByUserId == uid), x => x.CreatedByUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.ChatMessages.Where(x => x.SenderId == uid), x => x.SenderId = adminId, cancellationToken);
        await ReassignAsync(dbContext.MediaAssets.Where(x => x.UploadedByUserId == uid), x => x.UploadedByUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.LiveSessions.Where(x => x.HostUserId == uid), x => x.HostUserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.Appointments.Where(x => x.TeacherId == uid), x => x.TeacherId = adminId, cancellationToken);
        await ReassignAsync(dbContext.TeacherPayrollAdjustments.Where(x => x.TeacherId == uid), x => x.TeacherId = adminId, cancellationToken);
        await ReassignAsync(dbContext.StudentAskedQuestions.Where(x => x.StudentId == uid), x => x.StudentId = adminId, cancellationToken);
        await ReassignAsync(dbContext.ClassroomCourses.Where(x => x.TeacherId == uid), x => x.TeacherId = adminId, cancellationToken);
        await ReassignAsync(dbContext.StudentClassroomAttendances.Where(x => x.RecordedByTeacherId == uid), x => x.RecordedByTeacherId = adminId, cancellationToken);
        await ReassignAsync(dbContext.QuizAttempts.Where(x => x.UserId == uid), x => x.UserId = adminId, cancellationToken);
        await ReassignAsync(dbContext.UserNotifications.Where(x => x.UserId == uid), x => x.UserId = adminId, cancellationToken);

        await ReassignUniqueAsync(
            dbContext.StudentProgress.Where(x => x.UserId == uid),
            dbContext.StudentProgress.Where(x => x.UserId == adminId),
            x => x.StepId,
            x => x.UserId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.UserBadges.Where(x => x.UserId == uid),
            dbContext.UserBadges.Where(x => x.UserId == adminId),
            x => x.BadgeId,
            x => x.UserId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.FixedTimetableEntries.Where(x => x.TeacherId == uid),
            dbContext.FixedTimetableEntries.Where(x => x.TeacherId == adminId),
            x => (x.DayOfWeek, x.Period, x.SessionNumber),
            x => x.TeacherId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.TeacherSessionAttendances.Where(x => x.TeacherId == uid),
            dbContext.TeacherSessionAttendances.Where(x => x.TeacherId == adminId),
            x => (x.CourseId, x.SessionDate),
            x => x.TeacherId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.StudentClassroomAttendances.Where(x => x.StudentId == uid),
            dbContext.StudentClassroomAttendances.Where(x => x.StudentId == adminId),
            x => (x.ClassroomId, x.AttendanceDate),
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.StudentWeeklyReports.Where(x => x.TeacherId == uid),
            dbContext.StudentWeeklyReports.Where(x => x.TeacherId == adminId),
            x => (x.StudentId, x.WeekStartDate),
            x => x.TeacherId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.StudentWeeklyReports.Where(x => x.StudentId == uid),
            dbContext.StudentWeeklyReports.Where(x => x.StudentId == adminId),
            x => (x.TeacherId, x.WeekStartDate),
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.WeeklyStudyPlans.Where(x => x.TeacherId == uid),
            dbContext.WeeklyStudyPlans.Where(x => x.TeacherId == adminId),
            x => (x.CourseId, x.FromDate),
            x => x.TeacherId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.TeacherCourseRates.Where(x => x.TeacherId == uid),
            dbContext.TeacherCourseRates.Where(x => x.TeacherId == adminId),
            x => x.CourseId,
            x => x.TeacherId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.ClassroomStudents.Where(x => x.StudentId == uid),
            dbContext.ClassroomStudents.Where(x => x.StudentId == adminId),
            x => x.ClassroomId,
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.StudentCourseEnrollments.Where(x => x.StudentId == uid),
            dbContext.StudentCourseEnrollments.Where(x => x.StudentId == adminId),
            x => (x.ClassroomId, x.CourseId),
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.AssignmentSubmissions.Where(x => x.StudentId == uid),
            dbContext.AssignmentSubmissions.Where(x => x.StudentId == adminId),
            x => x.AssignmentId,
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.ExamAttempts.Where(x => x.StudentId == uid),
            dbContext.ExamAttempts.Where(x => x.StudentId == adminId),
            x => x.ExamId,
            x => x.StudentId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.ChatRoomMembers.Where(x => x.UserId == uid),
            dbContext.ChatRoomMembers.Where(x => x.UserId == adminId),
            x => x.RoomId,
            x => x.UserId = adminId,
            cancellationToken);
        await ReassignUniqueAsync(
            dbContext.VideoWatchSessions.Where(x => x.StudentId == uid),
            dbContext.VideoWatchSessions.Where(x => x.StudentId == adminId),
            x => x.MediaAssetId,
            x => x.StudentId = adminId,
            cancellationToken);

        dbContext.PasswordResetTokens.RemoveRange(
            await dbContext.PasswordResetTokens.Where(x => x.UserId == uid).ToListAsync(cancellationToken));

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task ReassignAsync<T>(IQueryable<T> query, Action<T> assign, CancellationToken cancellationToken)
        where T : class
    {
        foreach (var row in await query.ToListAsync(cancellationToken))
        {
            assign(row);
        }
    }

    private static async Task ReassignUniqueAsync<T, TKey>(
        IQueryable<T> query,
        IQueryable<T> adminQuery,
        Func<T, TKey> key,
        Action<T> assign,
        CancellationToken cancellationToken)
        where T : class
    {
        var taken = (await adminQuery.ToListAsync(cancellationToken)).Select(key).ToHashSet();
        foreach (var row in await query.ToListAsync(cancellationToken))
        {
            if (!taken.Add(key(row)))
            {
                continue;
            }

            assign(row);
        }
    }
}
