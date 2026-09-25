using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

internal static class LearningMaterialFlagsLoader
{
    public static async Task<LearningMaterialFlags> LoadAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        var courseSet = new HashSet<Guid>();
        var unitSet = new HashSet<Guid>();
        var lessonSet = new HashSet<Guid>();
        if (courseIds.Count == 0)
        {
            return new LearningMaterialFlags(courseSet, unitSet, lessonSet);
        }

        var rows = await dbContext.LearningMaterials
            .AsNoTracking()
            .Where(x => courseIds.Contains(x.CourseId))
            .Select(x => new { x.CourseId, x.UnitId, x.LessonId })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.LessonId is Guid lessonId)
            {
                lessonSet.Add(lessonId);
            }
            else if (row.UnitId is Guid unitId)
            {
                unitSet.Add(unitId);
            }
            else
            {
                courseSet.Add(row.CourseId);
            }
        }

        return new LearningMaterialFlags(courseSet, unitSet, lessonSet);
    }
}

internal sealed record LearningMaterialFlags(
    IReadOnlySet<Guid> CourseIds,
    IReadOnlySet<Guid> UnitIds,
    IReadOnlySet<Guid> LessonIds);
