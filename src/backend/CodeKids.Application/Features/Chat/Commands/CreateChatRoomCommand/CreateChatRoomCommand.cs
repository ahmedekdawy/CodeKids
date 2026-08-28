using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Chat;

public sealed record CreateChatRoomCommand(
    Guid TeacherId,
    Guid ClassroomId,
    Guid CourseId,
    Guid? UnitId,
    Guid? LessonId,
    ChatKind Kind,
    IReadOnlyList<Guid> StudentIds) : ICommand<ChatRoomDto>;
