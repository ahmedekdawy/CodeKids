using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class LearningMaterial : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LessonId { get; set; }
    public Guid MediaAssetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public LearningMaterialKind Kind { get; set; }
    public int SortOrder { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Course? Course { get; set; }
    public MediaAsset? MediaAsset { get; set; }
    public User? UploadedBy { get; set; }
}
