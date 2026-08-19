using System.Text.Json.Serialization;

namespace CodeKids.Application.Features.Dashboard;

public sealed record ChildProgressDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    string MobilePhone,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    int? Grade,
    int TotalXp,
    int CompletedSteps,
    int QuizAttempts,
    Guid? AvatarId,
    IReadOnlyList<string> Badges,
    ChildEvaluationSummaryDto? LatestEvaluation);
