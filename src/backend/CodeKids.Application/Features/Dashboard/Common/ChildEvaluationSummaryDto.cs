namespace CodeKids.Application.Features.Dashboard;

public sealed record ChildEvaluationSummaryDto(
    DateOnly WeekStartDate,
    string? TeacherName,
    int? PerformancePercent,
    int? AttendancePercent,
    int? HomeworkPercent,
    string InteractionDuringSession,
    bool? OpenCamera);
