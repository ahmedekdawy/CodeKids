using CodeKids.Application.Abstractions;
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
            .Include(x => x.Lesson)!.ThenInclude(l => l!.Course)
            .Where(x => isAdmin || x.MediaAsset!.UploadedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TeacherLessonVideoDto(
                x.Id,
                x.LessonId,
                x.Lesson!.Title,
                x.Lesson.CourseId,
                x.Lesson.Course!.Title,
                x.MediaAssetId,
                x.Title,
                x.MediaAsset!.FileName,
                x.MediaAsset.SizeBytes,
                x.MediaAsset.DurationSeconds,
                x.SortOrder,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

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

        return new TeacherVideoLibraryDto(lessonVideos, solutionVideos);
    }
}
