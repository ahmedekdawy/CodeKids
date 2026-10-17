namespace CodeKids.Domain.Entities;

/// <summary>Assignment of a user to a role. A user can belong to several roles.</summary>
public class UserInRole : TenantEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
