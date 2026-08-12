using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class GetZoomConnectUrlQueryHandler(
    IAppDbContext dbContext,
    IZoomUserOAuthService zoomUserOAuth) : IQueryHandler<GetZoomConnectUrlQuery, ZoomConnectUrlDto>
{
    public async Task<ZoomConnectUrlDto> Handle(GetZoomConnectUrlQuery query, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.TeacherUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        if (!zoomUserOAuth.IsUserOAuthConfigured)
        {
            return new ZoomConnectUrlDto(string.Empty, false);
        }

        return new ZoomConnectUrlDto(zoomUserOAuth.BuildAuthorizeUrl(query.TeacherUserId), true);
    }
}
