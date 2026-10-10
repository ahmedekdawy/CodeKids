using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class DisconnectZoomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DisconnectZoomCommand, bool>
{
    public async Task<bool> Handle(DisconnectZoomCommand command, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == command.TeacherUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        teacher.EnsureTeacherProfile().ZoomAccessToken = string.Empty;
        teacher.EnsureTeacherProfile().ZoomRefreshToken = string.Empty;
        teacher.EnsureTeacherProfile().ZoomTokenExpiresAt = null;
        teacher.EnsureTeacherProfile().ZoomConnectedEmail = string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
