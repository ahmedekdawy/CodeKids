namespace CodeKids.Domain.Entities;

public class MediaAsset : TenantEntity
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    /// <summary>When set, playback uses this URL instead of local file storage.</summary>
    public string? ExternalUrl { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? UploadedBy { get; set; }
}

public class LessonVideo : TenantEntity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public Guid MediaAssetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Lesson? Lesson { get; set; }
    public MediaAsset? MediaAsset { get; set; }
}

public class VideoWatchSession : TenantEntity
{
    public Guid Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? LessonId { get; set; }
    public int ActualWatchSeconds { get; set; }
    public int MaxPositionSeconds { get; set; }
    public bool UsedSpeedUp { get; set; }
    public bool SkippedAhead { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastEventAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public MediaAsset? MediaAsset { get; set; }
    public User? Student { get; set; }
    public Lesson? Lesson { get; set; }
}

public class WhatsAppReportLog : TenantEntity
{
    public Guid Id { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? StudentId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessagePreview { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
