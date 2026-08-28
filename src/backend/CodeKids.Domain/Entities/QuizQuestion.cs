namespace CodeKids.Domain.Entities;

public class QuizQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    /// <summary>JSON array of { key, text } for dynamic choice options.</summary>
    public string OptionsJson { get; set; } = "[]";
    public string CorrectOption { get; set; } = "A";
    public int SortOrder { get; set; }
    public Guid? PromptImageMediaAssetId { get; set; }
    public Quiz? Quiz { get; set; }
    public MediaAsset? PromptImage { get; set; }
}

