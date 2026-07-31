namespace CodeKids.Domain.Entities;

public class Quiz
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XpReward { get; set; }
    public Course? Course { get; set; }
    public Classroom? Classroom { get; set; }
    public User? CreatedBy { get; set; }
    public List<QuizQuestion> Questions { get; set; } = [];
}
