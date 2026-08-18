using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Attendance;

public sealed record ListTeacherPayrollAdjustmentsQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? TeacherId) : IQuery<IReadOnlyList<TeacherPayrollAdjustmentDto>>;

public sealed record CreateTeacherPayrollAdjustmentCommand(
    Guid TeacherId,
    decimal Amount,
    DateOnly AdjustmentDate,
    string Notes) : ICommand<TeacherPayrollAdjustmentDto>;

public sealed record DeleteTeacherPayrollAdjustmentCommand(Guid AdjustmentId) : ICommand<bool>;
