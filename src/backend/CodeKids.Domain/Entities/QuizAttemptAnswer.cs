namespace CodeKids.Domain.Entities;

public class QuizAttemptAnswer
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string SelectedOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public QuizAttempt? Attempt { get; set; }
    public QuizQuestion? Question { get; set; }
}
