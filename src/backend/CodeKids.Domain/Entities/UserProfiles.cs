using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

/// <summary>Student-only data, one row per student. Shares its key with <see cref="User"/>.</summary>
public class StudentProfile
{
    public const int MinSessionMinutes = 5;
    public const int MaxSessionMinutes = 600;
    public const int MinSessionsPerWeek = 1;
    public const int MaxSessionsPerWeek = 50;

    public Guid UserId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? AvatarId { get; set; }
    /// <summary>Student grade: KG1=-1, KG2=0, 1–12; null when unset.</summary>
    public int? Grade { get; set; }
    /// <summary>Student school type: Arabic or Language; null when unset.</summary>
    public SchoolType? SchoolType { get; set; }
    public int TotalXp { get; set; }
    /// <summary>Length of one of this student's sessions, in minutes; null when not set.</summary>
    public int? SessionMinutes { get; set; }
    /// <summary>How many sessions the student takes each week; null when not set.</summary>
    public int? SessionsPerWeek { get; set; }
    public Gender? Gender { get; set; }
    /// <summary>IANA time zone id (e.g. Africa/Cairo); only used when the tenant allows multiple time zones.</summary>
    public string? TimeZone { get; set; }

    public User? User { get; set; }
    public User? Parent { get; set; }
    public Avatar? Avatar { get; set; }
}

/// <summary>Teacher-only data, one row per teacher. Shares its key with <see cref="User"/>.</summary>
public class TeacherProfile
{
    public Guid UserId { get; set; }
    /// <summary>Teacher work period: Am, Pm, or Both.</summary>
    public TeacherWorkShift? WorkShift { get; set; }
    /// <summary>Comma-separated stage codes (0–3).</summary>
    public string Stages { get; set; } = string.Empty;
    /// <summary>Teacher pay contract: Session or Monthly.</summary>
    public TeacherContractType? ContractType { get; set; }
    /// <summary>Default primary session amount.</summary>
    public decimal? PrimaryAmount { get; set; }
    /// <summary>Default preparatory (إعدادي) session amount.</summary>
    public decimal? PrepAmount { get; set; }
    /// <summary>Default secondary session amount.</summary>
    public decimal? SecondaryAmount { get; set; }
    /// <summary>Teacher monthly base salary; applied per calendar month in payroll range.</summary>
    public decimal? MonthlySalary { get; set; }
    public string ZoomAccessToken { get; set; } = string.Empty;
    public string ZoomRefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? ZoomTokenExpiresAt { get; set; }
    public string ZoomConnectedEmail { get; set; } = string.Empty;
    public Gender? Gender { get; set; }
    /// <summary>IANA time zone id (e.g. Africa/Cairo); only used when the tenant allows multiple time zones.</summary>
    public string? TimeZone { get; set; }

    public User? User { get; set; }

    public bool HasPersonalZoom =>
        !string.IsNullOrWhiteSpace(ZoomAccessToken) && !string.IsNullOrWhiteSpace(ZoomRefreshToken);
}

/// <summary>Parent-only data, one row per parent. Shares its key with <see cref="User"/>.</summary>
public class ParentProfile
{
    public Guid UserId { get; set; }

    public User? User { get; set; }
}
