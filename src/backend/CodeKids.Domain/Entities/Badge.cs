namespace CodeKids.Domain.Entities;

public class Badge
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int RequiredXp { get; set; }
    public int RequiredSteps { get; set; }
}

