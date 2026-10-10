using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class GetZoomStatusQueryHandler(
    IAppDbContext dbContext,
    IZoomUserOAuthService zoomUserOAuth) : IQueryHandler<GetZoomStatusQuery, ZoomConnectionStatus>
{
    public async Task<ZoomConnectionStatus> Handle(GetZoomStatusQuery query, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.TeacherUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        return new ZoomConnectionStatus(
            (teacher.TeacherProfile?.HasPersonalZoom ?? false),
            string.IsNullOrWhiteSpace(teacher.TeacherProfile?.ZoomConnectedEmail) ? null : teacher.TeacherProfile?.ZoomConnectedEmail,
            teacher.TeacherProfile?.ZoomTokenExpiresAt,
            zoomUserOAuth.IsServerOAuthConfigured,
            zoomUserOAuth.IsUserOAuthConfigured,
            zoomUserOAuth.UserOAuthRedirectUri,
            zoomUserOAuth.MaskedClientId);
    }
}
