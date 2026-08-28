using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public static class ChatAccess
{
    public static async Task EnsureTeacherCourseAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid classroomId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var assigned = await dbContext.ClassroomCourses
            .AsNoTracking()
            .AnyAsync(x => x.ClassroomId == classroomId && x.CourseId == courseId && x.TeacherId == teacherId, cancellationToken);
        if (!assigned)
        {
            throw new InvalidOperationException("You can only chat in classrooms and courses assigned to you.");
        }
    }

    public static async Task<ChatRoomMember> RequireMemberAsync(
        IAppDbContext dbContext,
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ChatRoomMembers
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("You are not in this chat.");
    }

    public static async Task EnsureCanSendAsync(
        IAppDbContext dbContext,
        ChatRoomMember member,
        string? role,
        CancellationToken cancellationToken)
    {
        if (string.Equals(role, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase) && member.IsBlocked)
        {
            throw new InvalidOperationException("You are blocked from this chat.");
        }

        await Task.CompletedTask;
    }

    public static bool CanModerate(string? role) =>
        string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

    public static ChatRoomDto ToDto(
        ChatRoom room,
        Guid viewerId,
        IReadOnlyDictionary<Guid, UserRole> roles,
        int unreadCount = 0)
    {
        var me = room.Members.FirstOrDefault(x => x.UserId == viewerId);
        return new ChatRoomDto(
            room.Id,
            room.ClassroomId,
            room.Classroom?.Name ?? string.Empty,
            room.CourseId,
            room.CourseTitle,
            room.UnitId,
            room.UnitTitle,
            room.LessonId,
            room.LessonTitle,
            room.Kind,
            room.Title,
            me?.IsBlocked ?? false,
            unreadCount,
            room.Members
                .OrderBy(x => x.User?.DisplayName)
                .Select(x => new ChatMemberDto(
                    x.UserId,
                    x.User?.DisplayName ?? string.Empty,
                    roles.GetValueOrDefault(x.UserId).ToString(),
                    x.IsBlocked))
                .ToList());
    }

    public static async Task<Dictionary<Guid, int>> UnreadCountsAsync(
        IAppDbContext dbContext,
        Guid userId,
        IEnumerable<Guid> roomIds,
        CancellationToken cancellationToken)
    {
        var ids = roomIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (
            from m in dbContext.ChatMessages.AsNoTracking()
            join mem in dbContext.ChatRoomMembers.AsNoTracking()
                on new { m.RoomId, UserId = userId } equals new { mem.RoomId, mem.UserId }
            where ids.Contains(m.RoomId)
                  && !m.IsDeleted
                  && m.SenderId != userId
                  && (mem.LastReadAtUtc == null || m.CreatedAtUtc > mem.LastReadAtUtc)
            group m by m.RoomId
            into g
            select new { RoomId = g.Key, Count = g.Count() }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.RoomId, x => x.Count);
    }

    public static ChatMessageDto ToDto(ChatMessage message, string senderName) =>
        new(
            message.Id,
            message.RoomId,
            message.SenderId,
            senderName,
            message.IsDeleted ? string.Empty : message.Body,
            message.CreatedAtUtc,
            message.IsDeleted);

    public static async Task<HashSet<Guid>> ClassroomStudentIdsForCourseAsync(
        IAppDbContext dbContext,
        Guid classroomId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.ClassroomId == classroomId)
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken);

        var visible = new HashSet<Guid>();
        foreach (var studentId in studentIds)
        {
            var courses = await StudentCourseVisibility.GetVisibleCourseIdsAsync(dbContext, studentId, cancellationToken);
            if (courses.Contains(courseId))
            {
                visible.Add(studentId);
            }
        }

        return visible;
    }
}
