namespace CodeKids.Domain.Entities;

/// <summary>
/// Tuition/fee payment recorded for a parent, or for a student who has no parent.
/// </summary>
public class TuitionPayment
{
    public Guid Id { get; set; }
    /// <summary>Set when the payer is a parent.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>Set when the payer is a student without a parent.</summary>
    public Guid? StudentId { get; set; }
    /// <summary>Billing year (e.g. 2026).</summary>
    public int Year { get; set; }
    /// <summary>Billing month (1–12).</summary>
    public int Month { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Calendar date the payment was received.</summary>
    public DateOnly PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? Parent { get; set; }
    public User? Student { get; set; }
}
