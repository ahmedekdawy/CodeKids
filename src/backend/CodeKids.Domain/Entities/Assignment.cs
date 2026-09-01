using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class Assignment : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? DueAtUtc { get; set; }
    public int XpReward { get; set; }
    public bool IsPublished { get; set; }
    public Guid? SolutionVideoMediaAssetId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Classroom? Classroom { get; set; }
    public User? CreatedBy { get; set; }
    public MediaAsset? SolutionVideo { get; set; }
    public List<AssignmentQuestion> Questions { get; set; } = [];
    public List<AssignmentSubmission> Submissions { get; set; } = [];
}

public class AssignmentQuestion : TenantEntity
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public AssignmentQuestionType QuestionType { get; set; }
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int SortOrder { get; set; }
    public Guid? PromptImageMediaAssetId { get; set; }

    public Assignment? Assignment { get; set; }
    public MediaAsset? PromptImage { get; set; }
}

public class AssignmentSubmission : TenantEntity
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public AssignmentSubmissionStatus Status { get; set; } = AssignmentSubmissionStatus.Submitted;
    public int? Score { get; set; }
    public int? MaxScore { get; set; }
    public string? TeacherFeedback { get; set; }
    public Guid? FeedbackImageMediaAssetId { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? GradedAtUtc { get; set; }

    public Assignment? Assignment { get; set; }
    public User? Student { get; set; }
    public MediaAsset? FeedbackImage { get; set; }
    public List<AssignmentAnswer> Answers { get; set; } = [];
}

public class AssignmentAnswer : TenantEntity
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public Guid? AnswerImageMediaAssetId { get; set; }
    public bool? IsCorrect { get; set; }
    public int? PointsAwarded { get; set; }

    public AssignmentSubmission? Submission { get; set; }
    public AssignmentQuestion? Question { get; set; }
    public MediaAsset? AnswerImage { get; set; }
}
