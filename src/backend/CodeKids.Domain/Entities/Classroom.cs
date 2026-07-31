namespace CodeKids.Domain.Entities;

public class Classroom
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public Guid? CourseId { get; set; }
    public string WhatsAppGroupInviteUrl { get; set; } = string.Empty;
    /// <summary>Comma-separated E.164 phone numbers notified via WhatsApp Cloud API.</summary>
    public string WhatsAppNotifyPhones { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
    public Course? Course { get; set; }
    public List<ClassroomStudent> Students { get; set; } = [];
}
