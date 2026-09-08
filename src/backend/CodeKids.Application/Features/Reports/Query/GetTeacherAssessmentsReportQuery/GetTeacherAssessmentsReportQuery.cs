using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Reports;

public sealed record GetTeacherAssessmentsReportQuery(
    Guid? TeacherId = null,
    Guid? ClassroomId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Status = null,
    string? Kind = null,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedTeacherAssessmentsResultDto>;
