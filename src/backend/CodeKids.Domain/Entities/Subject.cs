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
    /// <summary>Matches <see cref="Grade.Id"/> (1–12). Null on legacy rows that were stage-only.</summary>
    public int? GradeId { get; set; }
    public Grade? Grade { get; set; }
    /// <summary>1 = first term, 2 = second term. Null when the row is not term-specific.</summary>
    public int? TermId { get; set; }
    public string TrackCode { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string SourceTocUrl { get; set; } = string.Empty;
    public string Variants { get; set; } = string.Empty;
    public List<SubjectUnit> Units { get; set; } = [];
}
