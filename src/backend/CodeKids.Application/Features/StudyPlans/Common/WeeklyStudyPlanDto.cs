namespace CodeKids.Application.Features.StudyPlans;

public sealed record WeeklyStudyPlanTopicDto(
    Guid Id,
    string Title,
    bool Highlight,
    int SortOrder);

public sealed record WeeklyStudyPlanWeekDto(
    Guid Id,
    int WeekNumber,
    DateOnly FromDate,
    DateOnly ToDate,
    int SortOrder,
    IReadOnlyList<WeeklyStudyPlanTopicDto> Topics);

public sealed record WeeklyStudyPlanDto(
    Guid Id,
    Guid TeacherId,
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    string? CourseTerm,
    DateOnly FromDate,
    DateOnly ToDate,
    string Notes,
    IReadOnlyList<WeeklyStudyPlanWeekDto> Weeks);

public sealed record SaveWeeklyStudyPlanTopicDto(
    string Title,
    bool Highlight);

public sealed record SaveWeeklyStudyPlanWeekDto(
    int WeekNumber,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<SaveWeeklyStudyPlanTopicDto>? Topics);

public sealed record SaveWeeklyStudyPlanRequest(
    Guid? Id,
    Guid CourseId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Notes,
    IReadOnlyList<SaveWeeklyStudyPlanWeekDto>? Weeks);
