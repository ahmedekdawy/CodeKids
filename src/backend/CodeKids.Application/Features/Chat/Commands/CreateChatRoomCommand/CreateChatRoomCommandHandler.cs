using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Chat;

public sealed class CreateChatRoomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateChatRoomCommand, ChatRoomDto>
{
    public async Task<ChatRoomDto> Handle(CreateChatRoomCommand command, CancellationToken cancellationToken)
    {
        await ChatAccess.EnsureTeacherCourseAsync(
            dbContext, command.TeacherId, command.ClassroomId, command.CourseId, cancellationToken);

        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var (unitTitle, lessonTitle) = await ResolveScopeTitlesAsync(command, cancellationToken);
        var studentIds = await ResolveStudentIdsAsync(command, cancellationToken);
        if (studentIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one student.");
        }

        var existing = await FindExistingAsync(command, studentIds, cancellationToken);
        if (existing is not null)
        {
            await EnsureTeacherMemberAsync(existing.Id, command.TeacherId, cancellationToken);
            return await LoadDtoAsync(existing.Id, command.TeacherId, cancellationToken);
        }

        var kindLabel = command.Kind switch
        {
            ChatKind.Direct => "1:1",
            ChatKind.Group => "Group",
            _ => "Class"
        };
        var scope = string.Join(" · ", new[] { course.Title, unitTitle, lessonTitle }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var room = new ChatRoom
        {
            Id = Guid.NewGuid(),
            ClassroomId = command.ClassroomId,
            CourseId = command.CourseId,
            UnitId = command.UnitId,
            LessonId = command.LessonId,
            Kind = command.Kind,
            Title = $"{classroom.Name} · {scope} · {kindLabel}",
            CourseTitle = course.Title,
            UnitTitle = unitTitle,
            LessonTitle = lessonTitle,
            CreatedByUserId = command.TeacherId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.ChatRooms.Add(room);
        dbContext.ChatRoomMembers.Add(new ChatRoomMember { Id = Guid.NewGuid(), RoomId = room.Id, UserId = command.TeacherId });
        foreach (var studentId in studentIds)
        {
            dbContext.ChatRoomMembers.Add(new ChatRoomMember { Id = Guid.NewGuid(), RoomId = room.Id, UserId = studentId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadDtoAsync(room.Id, command.TeacherId, cancellationToken);
    }

    private async Task<(string UnitTitle, string LessonTitle)> ResolveScopeTitlesAsync(
        CreateChatRoomCommand command,
        CancellationToken cancellationToken)
    {
        var unitTitle = string.Empty;
        var lessonTitle = string.Empty;
        if (command.LessonId is Guid lessonId)
        {
            var found = await CourseOutlineResolver.FindLessonAsync(dbContext, lessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            if (found.Course.Id != command.CourseId)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected course.");
            }

            var lesson = CourseOutlineResolver.MapLesson(found.Course, found.Subject, found.Unit, found.Lesson);
            var unit = CourseOutlineResolver.MapUnit(found.Course, found.Subject, found.Unit);
            if (command.UnitId is Guid unitId && unit.Id != unitId)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected unit.");
            }

            return (unit.Title, lesson.Title);
        }

        if (command.UnitId is Guid onlyUnitId)
        {
            var found = await CourseOutlineResolver.FindUnitAsync(dbContext, onlyUnitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            if (found.Course.Id != command.CourseId)
            {
                throw new InvalidOperationException("Unit does not belong to the selected course.");
            }

            unitTitle = CourseOutlineResolver.MapUnit(found.Course, found.Subject, found.Unit).Title;
        }

        return (unitTitle, lessonTitle);
    }

    private async Task<List<Guid>> ResolveStudentIdsAsync(CreateChatRoomCommand command, CancellationToken cancellationToken)
    {
        var eligible = await ChatAccess.ClassroomStudentIdsForCourseAsync(
            dbContext, command.ClassroomId, command.CourseId, cancellationToken);

        if (command.Kind == ChatKind.Class)
        {
            return eligible.OrderBy(x => x).ToList();
        }

        var selected = (command.StudentIds ?? [])
            .Distinct()
            .Where(eligible.Contains)
            .ToList();

        if (command.Kind == ChatKind.Direct && selected.Count != 1)
        {
            throw new InvalidOperationException("Select one student for a direct chat.");
        }

        if (command.Kind == ChatKind.Group && selected.Count < 2)
        {
            throw new InvalidOperationException("Select at least two students for a group chat.");
        }

        return selected.OrderBy(x => x).ToList();
    }

    private async Task<ChatRoom?> FindExistingAsync(
        CreateChatRoomCommand command,
        List<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.ChatRooms
            .Include(x => x.Members)
            .Where(x =>
                x.ClassroomId == command.ClassroomId
                && x.CourseId == command.CourseId
                && x.Kind == command.Kind
                && x.UnitId == command.UnitId
                && x.LessonId == command.LessonId)
            .ToListAsync(cancellationToken);

        var wanted = studentIds.Append(command.TeacherId).OrderBy(x => x).ToArray();
        return candidates.FirstOrDefault(room =>
        {
            var members = room.Members.Select(m => m.UserId).OrderBy(x => x).ToArray();
            return members.SequenceEqual(wanted);
        });
    }

    private async Task EnsureTeacherMemberAsync(Guid roomId, Guid teacherId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ChatRoomMembers.AnyAsync(x => x.RoomId == roomId && x.UserId == teacherId, cancellationToken);
        if (exists) return;
        dbContext.ChatRoomMembers.Add(new ChatRoomMember { Id = Guid.NewGuid(), RoomId = roomId, UserId = teacherId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ChatRoomDto> LoadDtoAsync(Guid roomId, Guid viewerId, CancellationToken cancellationToken)
    {
        var room = await dbContext.ChatRooms
            .AsNoTracking()
            .Include(x => x.Classroom)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstAsync(x => x.Id == roomId, cancellationToken);
        var roles = await dbContext.Users
            .AsNoTracking()
            .Where(x => room.Members.Select(m => m.UserId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Role, cancellationToken);
        return ChatAccess.ToDto(room, viewerId, roles);
    }
}
