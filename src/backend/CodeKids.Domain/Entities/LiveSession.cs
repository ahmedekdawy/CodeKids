namespace CodeKids.Domain.Entities;

public class LiveSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid HostUserId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? ClassroomId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string ZoomMeetingId { get; set; } = string.Empty;
    public string JoinUrl { get; set; } = string.Empty;
    public string StartUrl { get; set; } = string.Empty;
    public bool WhatsAppNotified { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Host { get; set; }
    public Course? Course { get; set; }
    public Classroom? Classroom { get; set; }
}
