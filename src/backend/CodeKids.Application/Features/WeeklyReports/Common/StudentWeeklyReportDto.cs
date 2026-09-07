namespace CodeKids.Application.Features.WeeklyReports;

public sealed record StudentWeeklyReportDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid StudentId,
    string StudentName,
    int? StudentGrade,
    DateOnly WeekStartDate,
    int? PerformancePercent,
    int? AttendancePercent,
    int? HomeworkPercent,
    string InteractionDuringSession,
    bool? OpenCamera);

public sealed record StudentWeeklyReportGridRowDto(
    Guid? ReportId,
    Guid StudentId,
    string StudentName,
    int? StudentGrade,
    DateOnly WeekStartDate,
    int? PerformancePercent,
    int? AttendancePercent,
    int? HomeworkPercent,
    string InteractionDuringSession,
    bool? OpenCamera);

public sealed record SaveWeeklyReportEntryDto(
    Guid StudentId,
    int? PerformancePercent,
    int? AttendancePercent,
    int? HomeworkPercent,
    string InteractionDuringSession,
    bool? OpenCamera);

public sealed record SaveWeeklyReportsRequest(
    DateOnly WeekStartDate,
    IReadOnlyList<SaveWeeklyReportEntryDto> Entries);

/// <summary>
/// Public-facing row for distinguished students. <paramref name="PerformancePercent"/> is the
/// student's average across every subject they were evaluated in that week, and
/// <paramref name="SubjectCount"/> is how many subjects went into that average.
/// </summary>
public sealed record TopWeeklyStudentDto(
    Guid StudentId,
    string StudentName,
    int? StudentGrade,
    int PerformancePercent,
    int SubjectCount,
    string? ProfilePhotoUrl,
    DateOnly WeekStartDate);
