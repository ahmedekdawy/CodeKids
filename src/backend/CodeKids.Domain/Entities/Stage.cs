namespace CodeKids.Domain.Entities;

public class Stage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public List<Grade> Grades { get; set; } = [];
}
