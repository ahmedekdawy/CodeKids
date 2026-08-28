namespace CodeKids.Domain.Entities;

public class ChatRoomMember : TenantEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public bool IsBlocked { get; set; }
    public DateTimeOffset? BlockedAtUtc { get; set; }
    public Guid? BlockedByUserId { get; set; }
    public DateTimeOffset? LastReadAtUtc { get; set; }

    public ChatRoom? Room { get; set; }
    public User? User { get; set; }
}
