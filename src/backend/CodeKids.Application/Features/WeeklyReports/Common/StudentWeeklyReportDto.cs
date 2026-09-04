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

/// <summary>Public-facing row for distinguished students (performance ≥ 90).</summary>
public sealed record TopWeeklyStudentDto(
    string StudentName,
    int? StudentGrade,
    int PerformancePercent,
    DateOnly WeekStartDate);
