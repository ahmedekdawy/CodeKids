using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Chat;

public sealed record CreateChatRoomRequest(
    Guid ClassroomId,
    Guid CourseId,
    Guid? UnitId,
    Guid? LessonId,
    string Kind,
    IReadOnlyList<Guid>? StudentIds);

public sealed record SetChatMemberBlockedRequest(bool Blocked);

public sealed record SendChatMessageRequest(string Body);

public sealed record ChatMemberDto(
    Guid UserId,
    string DisplayName,
    string Role,
    bool IsBlocked);

public sealed record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderId,
    string SenderName,
    string Body,
    DateTimeOffset CreatedAtUtc,
    bool IsDeleted);

public sealed record ChatRoomDto(
    Guid Id,
    Guid ClassroomId,
    string ClassroomName,
    Guid CourseId,
    string CourseTitle,
    Guid? UnitId,
    string UnitTitle,
    Guid? LessonId,
    string LessonTitle,
    ChatKind Kind,
    string Title,
    bool IsBlocked,
    int UnreadCount,
    IReadOnlyList<ChatMemberDto> Members);

public sealed record ChatUnreadSummaryDto(
    int TotalUnread,
    Guid? RoomId,
    string RoomTitle);
