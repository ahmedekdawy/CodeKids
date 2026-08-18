namespace CodeKids.Domain.Entities;

public class QuizAttempt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuizId { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int EarnedXp { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public User? User { get; set; }
    public Quiz? Quiz { get; set; }
    public List<QuizAttemptAnswer> Answers { get; set; } = [];
}

