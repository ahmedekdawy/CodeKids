using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

/// <summary>
/// A named set of pages. The seeded SuperAdmin and Teacher roles are system roles: they cannot be
/// renamed, deleted, or have their pages changed. Admins can create additional roles on top of
/// either base role and choose the pages each one exposes.
/// </summary>
public class Role : TenantEntity
{
    public const string SuperAdminName = "SuperAdmin";
    public const string TeacherName = "Teacher";

    public Guid Id { get; set; }

    /// <summary>Stable machine name; unique per tenant.</summary>
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>True for the seeded SuperAdmin and Teacher roles.</summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Which panel members of this role use: SuperAdmin or Teacher. Only pages of that panel can be
    /// added to the role, so a role never mixes the two panels' routes.
    /// </summary>
    public UserRole BaseRole { get; set; } = UserRole.Teacher;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<RolePage> Pages { get; set; } = [];

    public List<UserInRole> Members { get; set; } = [];
}
