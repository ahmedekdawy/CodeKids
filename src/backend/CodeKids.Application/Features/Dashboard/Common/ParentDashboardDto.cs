namespace CodeKids.Application.Features.Dashboard;

public sealed record ParentDashboardDto(
    Guid ParentId,
    string ParentName,
    IReadOnlyList<ChildProgressDto> Children);
