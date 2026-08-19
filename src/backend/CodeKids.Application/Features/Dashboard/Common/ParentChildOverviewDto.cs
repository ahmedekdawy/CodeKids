namespace CodeKids.Application.Features.Dashboard;

public sealed record ParentAssessmentItemDto(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset? DueAtUtc,
    string Status,
    int? Score,
    int? MaxScore,
    string? TeacherFeedback,
    DateTimeOffset? CompletedAtUtc);

public sealed record ParentQuizItemDto(
    Guid Id,
    string Title,
    string Description,
    int XpReward,
    int TotalQuestions,
    int? Score,
    int? EarnedXp,
    DateTimeOffset? CompletedAtUtc);

public sealed record ParentChildCourseDto(
    Guid CourseId,
    string Title,
    string Theme,
    string Description,
    int? Grade,
    string? Term,
    IReadOnlyList<ParentAssessmentItemDto> Assignments,
    IReadOnlyList<ParentAssessmentItemDto> Exams,
    IReadOnlyList<ParentQuizItemDto> Quizzes);

public sealed record ParentChildOverviewDto(
    Guid StudentId,
    string DisplayName,
    int? Grade,
    IReadOnlyList<ChildEvaluationSummaryDto> Evaluations,
    IReadOnlyList<ParentChildCourseDto> Courses);
