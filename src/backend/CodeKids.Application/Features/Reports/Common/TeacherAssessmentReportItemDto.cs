namespace CodeKids.Application.Features.Reports;

public sealed record TeacherAssessmentReportItemDto(
    string Kind,
    Guid Id,
    string Title,
    Guid? TeacherId,
    string TeacherName,
    Guid? ClassroomId,
    string ClassroomName,
    string? CourseTitle,
    DateTimeOffset CreatedAtUtc,
    bool IsPublished);

public sealed record PagedTeacherAssessmentsResultDto(
    IReadOnlyList<TeacherAssessmentReportItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
