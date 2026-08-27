using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class GetTeacherVideoLibraryQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherVideoLibraryQuery, TeacherVideoLibraryDto>
{
    public async Task<TeacherVideoLibraryDto> Handle(
        GetTeacherVideoLibraryQuery query,
        CancellationToken cancellationToken)
    {
        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == query.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);

        var lessonVideos = await dbContext.LessonVideos
            .AsNoTracking()
            .Include(x => x.MediaAsset)
            .Where(x => isAdmin || x.MediaAsset!.UploadedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var lessonIndex = await CourseOutlineResolver.IndexLessonsAsync(dbContext, cancellationToken);
        var lessonVideoDtos = lessonVideos.Select(x =>
        {
            lessonIndex.TryGetValue(x.LessonId, out var found);
            return new TeacherLessonVideoDto(
                x.Id,
                x.LessonId,
                found.Lesson?.Title ?? "Lesson",
                found.Course?.Id ?? Guid.Empty,
                found.Course?.Title ?? "",
                x.MediaAssetId,
                x.Title,
                x.MediaAsset!.FileName,
                x.MediaAsset.SizeBytes,
                x.MediaAsset.DurationSeconds,
                x.SortOrder,
                x.CreatedAtUtc);
        }).ToList();

        var solutionVideos = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom)
            .Include(x => x.SolutionVideo)
            .Where(x => x.SolutionVideoMediaAssetId != null)
            .Where(x => isAdmin
                || x.CreatedByUserId == query.TeacherUserId
                || x.Classroom!.Courses.Any(t => t.TeacherId == query.TeacherUserId)
                || x.SolutionVideo!.UploadedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.SolutionVideo!.CreatedAtUtc)
            .Select(x => new TeacherSolutionVideoDto(
                x.Id,
                x.Title,
                x.ClassroomId,
                x.Classroom!.Name,
                x.SolutionVideoMediaAssetId!.Value,
                x.SolutionVideo!.FileName,
                x.SolutionVideo.SizeBytes,
                x.SolutionVideo.DurationSeconds,
                x.SolutionVideo.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new TeacherVideoLibraryDto(lessonVideoDtos, solutionVideos);
    }
}
