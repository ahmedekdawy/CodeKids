using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed class AttachLearningMaterialCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AttachLearningMaterialCommand, LearningMaterialDto>
{
    public async Task<LearningMaterialDto> Handle(AttachLearningMaterialCommand command, CancellationToken cancellationToken)
    {
        Guid courseId;
        Guid? unitId = null;
        Guid? lessonId = null;
        var titleFallback = "";

        if (command.LessonId is Guid resolvedLessonId)
        {
            var found = await CourseOutlineResolver.FindLessonAsync(dbContext, resolvedLessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            courseId = found.Course.Id;
            unitId = CourseOutlineResolver.UnitId(found.Course, found.Subject, found.Unit);
            lessonId = resolvedLessonId;
            titleFallback = found.Lesson.Title;
        }
        else if (command.UnitId is Guid resolvedUnitId)
        {
            var found = await CourseOutlineResolver.FindUnitAsync(dbContext, resolvedUnitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            courseId = found.Course.Id;
            unitId = resolvedUnitId;
            titleFallback = found.Unit.Title;
        }
        else if (command.CourseId is Guid resolvedCourseId)
        {
            courseId = resolvedCourseId;
        }
        else
        {
            throw new InvalidOperationException("Course, unit, or lesson is required.");
        }

        var courseQuery = dbContext.Courses.AsNoTracking().Where(x => x.Id == courseId);
        courseQuery = await CourseQueryFilter.ApplyRoleAsync(
            dbContext, courseQuery, command.TeacherUserId, command.Role, cancellationToken);
        var course = await courseQuery.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");
        titleFallback = string.IsNullOrWhiteSpace(titleFallback) ? course.Title : titleFallback;

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        if (media.UploadedByUserId != command.TeacherUserId)
        {
            var isAdmin = string.Equals(command.Role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
            if (!isAdmin)
            {
                throw new InvalidOperationException("You can only attach media you uploaded.");
            }
        }

        var contentType = Media.MediaFileTypes.ResolveContentType(media.ContentType, media.FileName);
        var kind = LearningMaterialUploadRules.KindFromContentType(contentType);

        var material = new LearningMaterial
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UnitId = unitId,
            LessonId = lessonId,
            MediaAssetId = media.Id,
            Title = string.IsNullOrWhiteSpace(command.Title) ? (string.IsNullOrWhiteSpace(media.FileName) ? titleFallback : media.FileName) : command.Title.Trim(),
            Kind = kind,
            SortOrder = command.SortOrder <= 0 ? 1 : command.SortOrder,
            UploadedByUserId = command.TeacherUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LearningMaterials.Add(material);
        await dbContext.SaveChangesAsync(cancellationToken);
        material.MediaAsset = media;
        return LearningMaterialMapper.ToDto(material);
    }
}
