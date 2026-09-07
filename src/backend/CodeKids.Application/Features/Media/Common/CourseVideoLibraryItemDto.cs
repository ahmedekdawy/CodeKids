namespace CodeKids.Application.Features.Media;

public sealed record CourseVideoLibraryItemDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid MediaAssetId,
    string Title,
    string FileName,
    long SizeBytes,
    int? DurationSeconds,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);
