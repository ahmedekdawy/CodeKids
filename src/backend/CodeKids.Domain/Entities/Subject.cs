namespace CodeKids.Domain.Entities;

public class Subject : TenantEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int StageId { get; set; }
    public Stage? Stage { get; set; }
}
