namespace CodeKids.Domain.Entities;

/// <summary>
/// Ad-hoc expense recorded by Super Admin (name, amount, date).
/// </summary>
public class OtherExpense
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Calendar date of the expense.</summary>
    public DateOnly ExpenseDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
