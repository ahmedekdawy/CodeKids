namespace CodeKids.Domain.Entities;

/// <summary>
/// A navigable page of the application. Pages are a fixed catalogue: admins pick them for roles,
/// they do not create them.
/// </summary>
public class Page : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>Route path, including the leading slash (for example "/teacher/overview"). Unique.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>i18n key of the menu label.</summary>
    public string LabelKey { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    /// <summary>i18n key of the menu category; empty when the page is not grouped.</summary>
    public string CategoryKey { get; set; } = string.Empty;

    /// <summary>Panel the page belongs to: "admin" or "teacher".</summary>
    public string Panel { get; set; } = string.Empty;

    /// <summary>Default position inside its panel.</summary>
    public int SortOrder { get; set; }

    public List<RolePage> Roles { get; set; } = [];

    public const string AdminPanel = "admin";
    public const string TeacherPanel = "teacher";
}
