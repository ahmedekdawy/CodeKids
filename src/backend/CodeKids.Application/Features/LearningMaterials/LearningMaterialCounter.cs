using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

internal static class LearningMaterialCounter
{
    /// <summary>Counts course-level, unit-level, and lesson-level materials per course in one query.</summary>
    public static async Task<Dictionary<Guid, int>> CountByCourseIdsAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        if (courseIds.Count == 0)
        {
            return [];
        }

        return await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x => courseIds.Contains(x.CourseId))
            .GroupBy(x => x.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);
    }
}
