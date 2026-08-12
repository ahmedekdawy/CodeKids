using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class DeleteAssignmentSolutionVideoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : ICommandHandler<DeleteAssignmentSolutionVideoCommand, bool>
{
    public async Task<bool> Handle(DeleteAssignmentSolutionVideoCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.SolutionVideo)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.SolutionVideoMediaAssetId is null)
        {
            throw new InvalidOperationException("Assignment has no solution video.");
        }

        var isAdmin = await dbContext.Users.AnyAsync(
            x => x.Id == command.TeacherUserId && x.Role == UserRole.SuperAdmin,
            cancellationToken);
        if (!isAdmin
            && assignment.Classroom?.Courses.Any(t => t.TeacherId == command.TeacherUserId) != true
            && assignment.CreatedByUserId != command.TeacherUserId
            && assignment.SolutionVideo?.UploadedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("Only the classroom teacher can delete this solution video.");
        }

        var mediaId = assignment.SolutionVideoMediaAssetId.Value;
        var storageKey = assignment.SolutionVideo?.StorageKey;

        assignment.SolutionVideoMediaAssetId = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await MediaCleanup.TryDeleteOrphanMediaAsync(dbContext, fileStorage, mediaId, storageKey, cancellationToken);
        return true;
    }
}
