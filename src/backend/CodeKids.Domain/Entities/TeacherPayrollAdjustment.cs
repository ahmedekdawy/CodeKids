namespace CodeKids.Domain.Entities;

/// <summary>Manual payroll addition or deduction for a teacher.</summary>
public class TeacherPayrollAdjustment
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Calendar date the adjustment applies to.</summary>
    public DateOnly AdjustmentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Teacher { get; set; }
}
