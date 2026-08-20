namespace CodeKids.Domain.Entities;

public class Grade
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int StageId { get; set; }
    public Stage? Stage { get; set; }
}
