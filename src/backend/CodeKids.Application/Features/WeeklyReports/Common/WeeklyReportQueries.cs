using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.WeeklyReports;

public sealed record GetWeeklyReportGridQuery(
    Guid TeacherId,
    DateOnly WeekStartDate,
    int? Grade) : IQuery<IReadOnlyList<StudentWeeklyReportGridRowDto>>;

public sealed record ListStudentWeeklyReportsQuery(
    Guid? TeacherId,
    int? Grade,
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<IReadOnlyList<StudentWeeklyReportDto>>;

public sealed record SaveWeeklyReportsCommand(
    Guid TeacherId,
    DateOnly WeekStartDate,
    IReadOnlyList<SaveWeeklyReportEntryDto> Entries) : ICommand<IReadOnlyList<StudentWeeklyReportGridRowDto>>;
