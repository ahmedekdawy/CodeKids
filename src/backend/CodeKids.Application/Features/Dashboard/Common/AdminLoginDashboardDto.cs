namespace CodeKids.Application.Features.Dashboard;

public sealed record AdminLoginDashboardDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TeacherCount,
    int ParentCount,
    int StudentCount,
    IReadOnlyList<AdminLoginDashboardDayDto> Days);

public sealed record AdminLoginDashboardDayDto(
    DateOnly Date,
    int Teachers,
    int Parents,
    int Students);
