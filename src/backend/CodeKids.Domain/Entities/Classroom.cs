namespace CodeKids.Domain.Entities;

public class Classroom
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Optional classroom grade (KG1=-1, KG2=0, 1–12); null = unset.</summary>
    public int? Grade { get; set; }
    public Guid? CourseId { get; set; }
    public string WhatsAppGroupInviteUrl { get; set; } = string.Empty;
    /// <summary>Comma-separated E.164 phone numbers notified via WhatsApp Cloud API.</summary>
    public string WhatsAppNotifyPhones { get; set; } = string.Empty;
    public bool DailyWhatsAppReportsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Course? Course { get; set; }
    public List<ClassroomCourse> Courses { get; set; } = [];
    public List<ClassroomStudent> Students { get; set; } = [];
    public List<StudentCourseEnrollment> CourseEnrollments { get; set; } = [];
}
