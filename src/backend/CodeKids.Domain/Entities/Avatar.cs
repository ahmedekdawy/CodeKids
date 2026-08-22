namespace CodeKids.Domain.Entities;

public class Avatar : TenantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public int UnlockXp { get; set; }
}

