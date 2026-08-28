using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class ChatRoom : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LessonId { get; set; }
    public ChatKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Classroom? Classroom { get; set; }
    public Course? Course { get; set; }
    public User? CreatedBy { get; set; }
    public List<ChatRoomMember> Members { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
}
