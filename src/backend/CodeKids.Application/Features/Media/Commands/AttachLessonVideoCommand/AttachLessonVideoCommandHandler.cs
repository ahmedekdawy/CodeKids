using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class AttachLessonVideoCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AttachLessonVideoCommand, LessonVideoDto>
{
    public async Task<LessonVideoDto> Handle(AttachLessonVideoCommand command, CancellationToken cancellationToken)
    {
        Guid courseId;
        Guid? lessonId = command.LessonId;

        if (lessonId is Guid resolvedLessonId)
        {
            var found = await CourseOutlineResolver.FindLessonAsync(dbContext, resolvedLessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            courseId = found.Course.Id;
        }
        else if (command.CourseId is Guid resolvedCourseId)
        {
            var courseExists = await dbContext.Courses.AnyAsync(x => x.Id == resolvedCourseId, cancellationToken);
            if (!courseExists)
            {
                throw new InvalidOperationException("Course not found.");
            }

            courseId = resolvedCourseId;
        }
        else
        {
            throw new InvalidOperationException("Course or lesson is required.");
        }

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        if (media.UploadedByUserId != command.TeacherUserId)
        {
            var isAdmin = await dbContext.Users.AnyAsync(
                x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
                cancellationToken);
            if (!isAdmin)
            {
                throw new InvalidOperationException("You can only attach media you uploaded.");
            }
        }

        var video = new LessonVideo
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            CourseId = courseId,
            MediaAssetId = media.Id,
            Title = string.IsNullOrWhiteSpace(command.Title) ? media.FileName : command.Title.Trim(),
            SortOrder = command.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LessonVideos.Add(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LessonVideoDto(
            video.Id,
            video.LessonId,
            video.MediaAssetId,
            video.Title,
            video.SortOrder,
            media.FileName,
            media.DurationSeconds);
    }
}
