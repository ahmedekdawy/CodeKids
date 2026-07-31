using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? AvatarId { get; set; }
    public string MobilePhone { get; set; } = string.Empty;
    public string ZoomAccessToken { get; set; } = string.Empty;
    public string ZoomRefreshToken { get; set; } = string.Empty;
    public DateTimeOffset? ZoomTokenExpiresAt { get; set; }
    public string ZoomConnectedEmail { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Parent { get; set; }
    public Avatar? Avatar { get; set; }
    public List<User> Children { get; set; } = [];
    public List<UserBadge> Badges { get; set; } = [];
    public List<StudentProgress> Progress { get; set; } = [];
    public List<QuizAttempt> QuizAttempts { get; set; } = [];

    public bool HasPersonalZoom =>
        !string.IsNullOrWhiteSpace(ZoomAccessToken) && !string.IsNullOrWhiteSpace(ZoomRefreshToken);
}
