using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record LessonVideoSummaryDto(
    Guid Id,
    Guid MediaAssetId,
    string Title,
    int SortOrder,
    int? DurationSeconds);
