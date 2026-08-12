using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class DeleteLessonVideoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<DeleteLessonVideoCommand, bool>
{
    public async Task<bool> Handle(DeleteLessonVideoCommand command, CancellationToken cancellationToken)
    {
        var video = await dbContext.LessonVideos
            .Include(x => x.MediaAsset)
            .FirstOrDefaultAsync(x => x.Id == command.LessonVideoId, cancellationToken)
            ?? throw new InvalidOperationException("Lesson video not found.");

        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin && video.MediaAsset?.UploadedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("You can only delete videos you uploaded.");
        }

        var mediaId = video.MediaAssetId;
        var storageKey = video.MediaAsset?.StorageKey;

        dbContext.LessonVideos.Remove(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        await MediaCleanup.TryDeleteOrphanMediaAsync(dbContext, fileStorage, mediaId, storageKey, cancellationToken);
        return true;
    }
}
