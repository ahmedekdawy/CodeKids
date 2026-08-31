namespace CodeKids.Application.Features.Courses;

public sealed record CourseVideoSummaryDto(
    Guid Id,
    Guid MediaAssetId,
    string Title,
    int SortOrder,
    int? DurationSeconds);
