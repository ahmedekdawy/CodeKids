namespace CodeKids.Domain.Entities;

/// <summary>A topic or activity listed inside a study-plan week.</summary>
public class WeeklyStudyPlanTopic
{
    public Guid Id { get; set; }
    public Guid WeeklyStudyPlanItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Highlight { get; set; }
    public int SortOrder { get; set; }

    public WeeklyStudyPlanItem? Week { get; set; }
}
