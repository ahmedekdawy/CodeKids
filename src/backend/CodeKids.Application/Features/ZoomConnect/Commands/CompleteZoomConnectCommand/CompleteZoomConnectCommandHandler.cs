using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed class CompleteZoomConnectCommandHandler(
    IAppDbContext dbContext,
    IZoomUserOAuthService zoomUserOAuth) : ICommandHandler<CompleteZoomConnectCommand, ZoomConnectResultDto>
{
    public async Task<ZoomConnectResultDto> Handle(CompleteZoomConnectCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.State))
        {
            throw new InvalidOperationException("Zoom OAuth code and state are required.");
        }

        if (!zoomUserOAuth.TryParseState(command.State, out var teacherUserId))
        {
            throw new InvalidOperationException("Invalid or expired Zoom OAuth state.");
        }

        var teacher = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == teacherUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        var tokens = await zoomUserOAuth.ExchangeCodeAsync(command.Code, cancellationToken);
        teacher.ZoomAccessToken = tokens.AccessToken;
        teacher.ZoomRefreshToken = tokens.RefreshToken;
        teacher.ZoomTokenExpiresAt = tokens.ExpiresAt;
        teacher.ZoomConnectedEmail = tokens.Email ?? teacher.Email;
        await dbContext.SaveChangesAsync(cancellationToken);

        var redirect = zoomUserOAuth.FrontendRedirectUri.TrimEnd('/');
        var separator = redirect.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return new ZoomConnectResultDto($"{redirect}{separator}zoom=connected");
    }
}
