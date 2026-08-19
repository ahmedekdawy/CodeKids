namespace CodeKids.Application.Features.Dashboard;

public sealed record ParentDashboardDto(
    Guid ParentId,
    string ParentName,
    string ParentEmail,
    string ParentMobilePhone,
    IReadOnlyList<ChildProgressDto> Children);
