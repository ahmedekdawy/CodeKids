using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class QuizQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid? ParentQuestionId { get; set; }
    public BankQuestionType QuestionType { get; set; } = BankQuestionType.SingleChoice;
    public string Prompt { get; set; } = string.Empty;
    public string PassageText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    /// <summary>JSON array of { key, text } for dynamic choice options.</summary>
    public string OptionsJson { get; set; } = "[]";
    public string CorrectOption { get; set; } = "A";
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int SortOrder { get; set; }
    public Guid? PromptImageMediaAssetId { get; set; }
    public Quiz? Quiz { get; set; }
    public MediaAsset? PromptImage { get; set; }
    public QuizQuestion? Parent { get; set; }
    public List<QuizQuestion> Children { get; set; } = [];
}

