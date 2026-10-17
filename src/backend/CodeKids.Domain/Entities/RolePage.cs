namespace CodeKids.Domain.Entities;

/// <summary>A page exposed by a role, with the role's default menu order.</summary>
public class RolePage : TenantEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid PageId { get; set; }
    public Page? Page { get; set; }

    public int SortOrder { get; set; }
}
