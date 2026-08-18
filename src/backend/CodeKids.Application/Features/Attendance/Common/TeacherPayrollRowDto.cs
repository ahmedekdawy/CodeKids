namespace CodeKids.Application.Features.Attendance;

public sealed record TeacherPayrollRowDto(
    Guid TeacherId,
    string TeacherName,
    int PrimarySessions,
    int PrepSessions,
    int SecondarySessions,
    decimal SessionAmount,
    decimal MonthlySalary,
    decimal ManualAmount,
    decimal TotalAmount);

public sealed record TeacherPayrollAdjustmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    decimal Amount,
    DateOnly AdjustmentDate,
    string Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateTeacherPayrollAdjustmentRequest(
    Guid TeacherId,
    decimal Amount,
    DateOnly AdjustmentDate,
    string Notes);
