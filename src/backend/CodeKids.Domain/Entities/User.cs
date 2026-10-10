using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class User : TenantEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string MobilePhone { get; set; } = string.Empty;
    /// <summary>Storage key of the user-uploaded profile photo; null when no photo was uploaded.</summary>
    public string? ProfilePhotoStorageKey { get; set; }
    public string? ProfilePhotoContentType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Set on each successful login; null until the user has signed in at least once.</summary>
    public DateTimeOffset? LastLoginDateUtc { get; set; }

    /// <summary>Role-specific data; each is null unless the user has that role.</summary>
    public StudentProfile? StudentProfile { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
    public ParentProfile? ParentProfile { get; set; }
    /// <summary>Students whose parent is this user.</summary>
    public List<StudentProfile> Children { get; set; } = [];
    public List<UserBadge> Badges { get; set; } = [];
    public List<StudentProgress> Progress { get; set; } = [];
    public List<QuizAttempt> QuizAttempts { get; set; } = [];
    public List<TeacherCourseRate> CourseRates { get; set; } = [];

    /// <summary>Returns the student profile, creating it the first time student data is written.</summary>
    public StudentProfile EnsureStudentProfile() => StudentProfile ??= new StudentProfile { UserId = Id };

    /// <summary>Returns the teacher profile, creating it the first time teacher data is written.</summary>
    public TeacherProfile EnsureTeacherProfile() => TeacherProfile ??= new TeacherProfile { UserId = Id };

    /// <summary>Returns the parent profile, creating it if missing.</summary>
    public ParentProfile EnsureParentProfile() => ParentProfile ??= new ParentProfile { UserId = Id };

    /// <summary>Creates the profile row that goes with <see cref="Role"/> if it does not exist yet.</summary>
    public void EnsureRoleProfile()
    {
        switch (Role)
        {
            case UserRole.Student:
                EnsureStudentProfile();
                break;
            case UserRole.Teacher:
                EnsureTeacherProfile();
                break;
            case UserRole.Parent:
                EnsureParentProfile();
                break;
        }
    }
}
