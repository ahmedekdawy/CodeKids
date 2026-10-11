namespace CodeKids.Domain.Entities;

/// <summary>
/// One period on one weekday when a teacher or student is available. A user can have several per day.
/// Times are wall-clock minutes from midnight in the user's own time zone.
/// </summary>
public class UserAvailability : TenantEntity
{
    public const int MinutesPerDay = 24 * 60;
    public const int MaxPeriodsPerDay = 12;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }
    public int StartMinutes { get; set; }
    /// <summary>Exclusive end; greater than <see cref="StartMinutes"/>, at most 1440.</summary>
    public int EndMinutes { get; set; }

    public User? User { get; set; }
}

/// <summary>A date range (both ends inclusive) when a teacher is on vacation.</summary>
public class TeacherVacation : TenantEntity
{
    public const int MaxNoteLength = 200;

    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
}
