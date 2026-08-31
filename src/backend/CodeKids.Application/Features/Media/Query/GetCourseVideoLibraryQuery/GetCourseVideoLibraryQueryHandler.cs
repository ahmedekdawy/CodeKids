using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetCourseVideoLibraryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCourseVideoLibraryQuery, IReadOnlyList<CourseVideoLibraryItemDto>>
{
    public async Task<IReadOnlyList<CourseVideoLibraryItemDto>> Handle(
        GetCourseVideoLibraryQuery query,
        CancellationToken cancellationToken)
    {
        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == query.AdminUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin)
        {
            throw new InvalidOperationException("Only admins can view the course video library.");
        }

        return await dbContext.LessonVideos
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.MediaAsset)
            .Where(x => x.LessonId == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CourseVideoLibraryItemDto(
                x.Id,
                x.CourseId ?? Guid.Empty,
                x.Course != null ? x.Course.Title : "",
                x.MediaAssetId,
                x.Title,
                x.MediaAsset!.FileName,
                x.MediaAsset.SizeBytes,
                x.MediaAsset.DurationSeconds,
                x.SortOrder,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
