using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed class GetLearningMaterialPageQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLearningMaterialPageQuery, LearningMaterialPageDto>
{
    public async Task<LearningMaterialPageDto> Handle(GetLearningMaterialPageQuery query, CancellationToken cancellationToken)
    {
        Guid courseId;
        Guid? unitId = null;
        Guid? lessonId = null;
        string courseTitle;
        string? unitTitle = null;
        string? lessonTitle = null;

        if (query.LessonId is Guid resolvedLessonId)
        {
            var found = await CourseOutlineResolver.FindLessonAsync(dbContext, resolvedLessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            courseId = found.Course.Id;
            unitId = CourseOutlineResolver.UnitId(found.Course, found.Subject, found.Unit);
            lessonId = resolvedLessonId;
            courseTitle = found.Course.Title;
            unitTitle = found.Unit.Title;
            lessonTitle = found.Lesson.Title;
        }
        else if (query.UnitId is Guid resolvedUnitId)
        {
            var found = await CourseOutlineResolver.FindUnitAsync(dbContext, resolvedUnitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            courseId = found.Course.Id;
            unitId = resolvedUnitId;
            courseTitle = found.Course.Title;
            unitTitle = found.Unit.Title;
        }
        else if (query.CourseId is Guid resolvedCourseId)
        {
            courseId = resolvedCourseId;
            var course = await dbContext.Courses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");
            courseTitle = course.Title;
        }
        else
        {
            throw new InvalidOperationException("Course, unit, or lesson is required.");
        }

        var courseQuery = dbContext.Courses.AsNoTracking().Where(x => x.Id == courseId);
        courseQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, courseQuery, query.UserId, query.Role, cancellationToken);
        if (!await courseQuery.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Course not found.");
        }

        var materialsQuery = dbContext.LearningMaterials
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Where(x => x.CourseId == courseId);

        if (lessonId is Guid filterLessonId)
        {
            materialsQuery = materialsQuery.Where(x => x.LessonId == filterLessonId);
        }
        else if (unitId is Guid filterUnitId)
        {
            materialsQuery = materialsQuery.Where(x => x.UnitId == filterUnitId && x.LessonId == null);
        }
        else
        {
            materialsQuery = materialsQuery.Where(x => x.UnitId == null && x.LessonId == null);
        }

        var items = await materialsQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new LearningMaterialPageDto(
            LearningMaterialMapper.ScopeOf(unitId, lessonId),
            courseId,
            courseTitle,
            unitId,
            unitTitle,
            lessonId,
            lessonTitle,
            items.Select(LearningMaterialMapper.ToDto).ToList());
    }
}
