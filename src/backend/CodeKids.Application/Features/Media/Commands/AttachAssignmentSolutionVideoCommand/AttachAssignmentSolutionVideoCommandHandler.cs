using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed class AttachAssignmentSolutionVideoCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<AttachAssignmentSolutionVideoCommand, MediaAssetDto>
{
    public async Task<MediaAssetDto> Handle(
        AttachAssignmentSolutionVideoCommand command,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.Courses.Any(t => t.TeacherId == command.TeacherUserId) != true
            && assignment.CreatedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("Only the classroom teacher can attach a solution video.");
        }

        var media = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == command.MediaAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Media asset not found.");

        assignment.SolutionVideoMediaAssetId = media.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaAssetDto(
            media.Id,
            media.FileName,
            media.ContentType,
            media.SizeBytes,
            media.DurationSeconds,
            media.CreatedAtUtc,
            media.ExternalUrl);
    }
}
