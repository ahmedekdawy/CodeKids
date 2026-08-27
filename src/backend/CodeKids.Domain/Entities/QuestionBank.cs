using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class BankQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ParentQuestionId { get; set; }
    public BankQuestionType QuestionType { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string PassageText { get; set; } = string.Empty;
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    /// <summary>JSON array of { key, text } for dynamic choice options.</summary>
    public string OptionsJson { get; set; } = "[]";
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Course? Course { get; set; }
    public User? CreatedBy { get; set; }
    public BankQuestion? Parent { get; set; }
    public List<BankQuestion> Children { get; set; } = [];
}

public class Exam : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? DueAtUtc { get; set; }
    public int XpReward { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Classroom? Classroom { get; set; }
    public Course? Course { get; set; }
    public User? CreatedBy { get; set; }
    public List<ExamQuestion> Questions { get; set; } = [];
    public List<ExamAttempt> Attempts { get; set; } = [];
}

public class ExamQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid? BankQuestionId { get; set; }
    public Guid? ParentExamQuestionId { get; set; }
    public Guid? LessonId { get; set; }
    public BankQuestionType QuestionType { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string PassageText { get; set; } = string.Empty;
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    /// <summary>JSON array of { key, text } for dynamic choice options.</summary>
    public string OptionsJson { get; set; } = "[]";
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int SortOrder { get; set; }

    public Exam? Exam { get; set; }
    public BankQuestion? BankQuestion { get; set; }
    public ExamQuestion? Parent { get; set; }
    public List<ExamQuestion> Children { get; set; } = [];
}

public class ExamAttempt : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.Submitted;
    public int? Score { get; set; }
    public int? MaxScore { get; set; }
    public string? TeacherFeedback { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? GradedAtUtc { get; set; }

    public Exam? Exam { get; set; }
    public User? Student { get; set; }
    public List<ExamAnswer> Answers { get; set; } = [];

    public int? DurationSeconds =>
        SubmittedAtUtc is DateTimeOffset submitted
            ? (int)Math.Max(0, (submitted - StartedAtUtc).TotalSeconds)
            : null;
}

public class ExamAnswer : TenantEntity
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public Guid ExamQuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public int? PointsAwarded { get; set; }

    public ExamAttempt? Attempt { get; set; }
    public ExamQuestion? Question { get; set; }
}
