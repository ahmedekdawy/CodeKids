using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.ZoomConnect;

public sealed record GetZoomConnectUrlQuery(Guid TeacherUserId) : IQuery<ZoomConnectUrlDto>;
public sealed record ZoomConnectUrlDto(string AuthorizeUrl, bool UserOAuthConfigured);

public sealed record CompleteZoomConnectCommand(string Code, string State) : ICommand<ZoomConnectResultDto>;
public sealed record ZoomConnectResultDto(string FrontendRedirectUrl);

public sealed record GetZoomStatusQuery(Guid TeacherUserId) : IQuery<ZoomConnectionStatus>;
public sealed record DisconnectZoomCommand(Guid TeacherUserId) : ICommand<bool>;

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
            teacher.HasPersonalZoom,
            string.IsNullOrWhiteSpace(teacher.ZoomConnectedEmail) ? null : teacher.ZoomConnectedEmail,
            teacher.ZoomTokenExpiresAt,
            zoomUserOAuth.IsServerOAuthConfigured);
    }
}

public sealed class DisconnectZoomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DisconnectZoomCommand, bool>
{
    public async Task<bool> Handle(DisconnectZoomCommand command, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == command.TeacherUserId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        teacher.ZoomAccessToken = string.Empty;
        teacher.ZoomRefreshToken = string.Empty;
        teacher.ZoomTokenExpiresAt = null;
        teacher.ZoomConnectedEmail = string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
