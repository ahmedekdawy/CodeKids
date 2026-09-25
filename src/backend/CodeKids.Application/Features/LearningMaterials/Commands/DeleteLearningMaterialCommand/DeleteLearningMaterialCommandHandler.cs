using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.Media;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.LearningMaterials;

public sealed class DeleteLearningMaterialCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<DeleteLearningMaterialCommand, bool>
{
    public async Task<bool> Handle(DeleteLearningMaterialCommand command, CancellationToken cancellationToken)
    {
        var material = await dbContext.LearningMaterials
            .Include(x => x.MediaAsset)
            .FirstOrDefaultAsync(x => x.Id == command.MaterialId, cancellationToken)
            ?? throw new InvalidOperationException("Learning material not found.");

        var isAdmin = string.Equals(command.Role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && material.UploadedByUserId != command.TeacherUserId)
        {
            var courseQuery = dbContext.Courses.AsNoTracking().Where(x => x.Id == material.CourseId);
            courseQuery = await CourseQueryFilter.ApplyRoleAsync(
                dbContext, courseQuery, command.TeacherUserId, command.Role, cancellationToken);
            if (!await courseQuery.AnyAsync(cancellationToken))
            {
                throw new InvalidOperationException("You can only delete materials you uploaded.");
            }
        }

        var mediaId = material.MediaAssetId;
        var storageKey = material.MediaAsset?.StorageKey;

        dbContext.LearningMaterials.Remove(material);
        await dbContext.SaveChangesAsync(cancellationToken);
        await MediaCleanup.TryDeleteOrphanMediaAsync(dbContext, fileStorage, mediaId, storageKey, cancellationToken);
        return true;
    }
}
