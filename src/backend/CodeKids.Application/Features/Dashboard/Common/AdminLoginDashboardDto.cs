namespace CodeKids.Application.Features.Dashboard;

public sealed record AdminLoginDashboardDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TeacherCount,
    int ParentCount,
    int StudentCount,
    IReadOnlyList<AdminLoginDashboardDayDto> Days,
    IReadOnlyList<AdminLoginUserDto> Teachers,
    IReadOnlyList<AdminLoginUserDto> Parents,
    IReadOnlyList<AdminLoginUserDto> Students);

public sealed record AdminLoginDashboardDayDto(
    DateOnly Date,
    int Teachers,
    int Parents,
    int Students);

public sealed record AdminLoginUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    string MobilePhone,
    DateTimeOffset LastLoginDateUtc);
