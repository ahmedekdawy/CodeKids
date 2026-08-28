namespace CodeKids.Domain.Entities;

public class ChatMessage : TenantEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public ChatRoom? Room { get; set; }
    public User? Sender { get; set; }
}
