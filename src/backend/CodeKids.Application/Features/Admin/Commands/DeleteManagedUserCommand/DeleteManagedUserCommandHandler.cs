using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class DeleteManagedUserCommandHandler(IAppDbContext dbContext, IUserCascadeDeleter cascadeDeleter)
    : ICommandHandler<DeleteManagedUserCommand, bool>
{
    public async Task<bool> Handle(DeleteManagedUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == command.UserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsTenantOwner)
        {
            throw new InvalidOperationException("The account owner cannot be deleted.");
        }

        if (user.Role == UserRole.SuperAdmin)
        {
            var adminCount = await dbContext.Users.CountAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last Super Admin.");
            }
        }

        // A student's records (attempts, attendance, enrolments) mean nothing on anyone else's account,
        // so they always go with the student. For other roles the caller chooses.
        var deleteRelatedData = command.DeleteRelatedData || user.Role == UserRole.Student;
        if (!deleteRelatedData)
        {
            // Keep the user's data: hand it to the admin first, so the delete below finds little left to remove.
            await MoveDataToAdminAsync(user.Id, command.AdminUserId, cancellationToken);
        }

        // Removes the user and whatever still belongs to them: their profile, enrolments, attendance,
        // attempts and submissions, and anything they created (exams, assignments, questions, videos...)
        // along with what hangs off those. Optional links, such as a child's parent, are just cleared.
        await cascadeDeleter.DeleteUserAsync(user.Id, cancellationToken);

        // These columns hold a user id without a database relationship, so the cascade cannot see them.
        var uid = user.Id;
        await dbContext.UserNotifications.Where(x => x.RelatedStudentId == uid)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.RelatedStudentId, (Guid?)null), cancellationToken);
        await dbContext.ChatRoomMembers.Where(x => x.BlockedByUserId == uid)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.BlockedByUserId, (Guid?)null), cancellationToken);
        await dbContext.ChatMessages.Where(x => x.DeletedByUserId == uid)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.DeletedByUserId, (Guid?)null), cancellationToken);
        await dbContext.WhatsAppReportLogs.Where(x => x.StudentId == uid)
            .ExecuteUpdateAsync(x => x.SetProperty(l => l.StudentId, (Guid?)null), cancellationToken);
        return true;
    }

    /// <summary>
    /// Moves the user's rows to the admin. Where the admin already has the same row (same classroom,
    /// same exam, same day...) the user's row is left behind and goes with the user.
    /// </summary>
    private async Task MoveDataToAdminAsync(Guid uid, Guid adminId, CancellationToken cancellationToken)
    {
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
        await ReassignAsync(dbContext.LearningMaterials.Where(x => x.UploadedByUserId == uid), x => x.UploadedByUserId = adminId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
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
