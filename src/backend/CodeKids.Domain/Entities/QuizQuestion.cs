namespace CodeKids.Domain.Entities;

public class QuizQuestion
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A";
    public int SortOrder { get; set; }
    public Quiz? Quiz { get; set; }
}

