using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class User : TenantEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? AvatarId { get; set; }
    /// <summary>Student grade: KG1=-1, KG2=0, 1–12; null when unset or not a student.</summary>
    public int? Grade { get; set; }
    /// <summary>Student school type: Arabic or Language; null when unset or not a student.</summary>
    public SchoolType? SchoolType { get; set; }
    public string MobilePhone { get; set; } = string.Empty;
    /// <summary>Teacher work period: Am, Pm, or Both; null for non-teachers.</summary>
    public TeacherWorkShift? WorkShift { get; set; }
    /// <summary>Comma-separated stage codes (0–3) for teachers; empty for non-teachers.</summary>
    public string Stages { get; set; } = string.Empty;
    /// <summary>Teacher pay contract: Session or Monthly; null for non-teachers.</summary>
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
    public int TotalXp { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Set on each successful login; null until the user has signed in at least once.</summary>
    public DateTimeOffset? LastLoginDateUtc { get; set; }

    public User? Parent { get; set; }
    public Avatar? Avatar { get; set; }
    public List<User> Children { get; set; } = [];
    public List<UserBadge> Badges { get; set; } = [];
    public List<StudentProgress> Progress { get; set; } = [];
    public List<QuizAttempt> QuizAttempts { get; set; } = [];
    public List<TeacherCourseRate> CourseRates { get; set; } = [];

    public bool HasPersonalZoom =>
        !string.IsNullOrWhiteSpace(ZoomAccessToken) && !string.IsNullOrWhiteSpace(ZoomRefreshToken);
}
