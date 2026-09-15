using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed class GetTeacherLearningMaterialsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherLearningMaterialsQuery, IReadOnlyList<TeacherLearningMaterialDto>>
{
    public async Task<IReadOnlyList<TeacherLearningMaterialDto>> Handle(
        GetTeacherLearningMaterialsQuery query,
        CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses.AsNoTracking();
        coursesQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, coursesQuery, query.UserId, query.Role, cancellationToken);
        var courses = await coursesQuery.ToListAsync(cancellationToken);
        var courseIds = courses.Select(c => c.Id).ToHashSet();
        var titles = courses.ToDictionary(c => c.Id, c => c.Title);
        var outlines = await CourseOutlineResolver.ResolveManyAsync(dbContext, courses, cancellationToken);

        var materials = await dbContext.LearningMaterials
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Where(x => courseIds.Contains(x.CourseId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return materials.Select(material =>
        {
            outlines.TryGetValue(material.CourseId, out var outline);
            var unitTitle = material.UnitId is Guid unitId
                ? outline?.Units.FirstOrDefault(u => u.Id == unitId)?.Title
                : null;
            var lessonTitle = material.LessonId is Guid lessonId
                ? outline?.Lessons.FirstOrDefault(l => l.Id == lessonId)?.Title
                : null;
            var media = material.MediaAsset;
            return new TeacherLearningMaterialDto(
                material.Id,
                LearningMaterialMapper.ScopeOf(material.UnitId, material.LessonId),
                material.CourseId,
                titles.GetValueOrDefault(material.CourseId, ""),
                material.UnitId,
                unitTitle,
                material.LessonId,
                lessonTitle,
                material.MediaAssetId,
                material.Title,
                material.Kind.ToString(),
                media?.FileName ?? "",
                media?.ContentType ?? "",
                media?.SizeBytes ?? 0,
                material.SortOrder,
                material.CreatedAtUtc);
        }).ToList();
    }
}
