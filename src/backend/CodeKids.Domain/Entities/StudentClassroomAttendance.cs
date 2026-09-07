using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

/// <summary>Daily student presence record for a classroom.</summary>
public class StudentClassroomAttendance : TenantEntity
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public StudentAttendanceStatus Status { get; set; } = StudentAttendanceStatus.Present;
    public Guid RecordedByTeacherId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Student { get; set; }
    public Classroom? Classroom { get; set; }
    public User? RecordedByTeacher { get; set; }
}
