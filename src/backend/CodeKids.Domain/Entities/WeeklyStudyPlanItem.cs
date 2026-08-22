namespace CodeKids.Domain.Entities;

/// <summary>One school week inside a course study plan.</summary>
public class WeeklyStudyPlanItem : TenantEntity
{
    public Guid Id { get; set; }
    public Guid WeeklyStudyPlanId { get; set; }
    public int WeekNumber { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int SortOrder { get; set; }

    public WeeklyStudyPlan? Plan { get; set; }
    public List<WeeklyStudyPlanTopic> Topics { get; set; } = [];
}
