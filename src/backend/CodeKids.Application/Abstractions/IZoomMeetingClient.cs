namespace CodeKids.Application.Abstractions;

public sealed record ZoomMeetingRequest(
    string Topic,
    string Agenda,
    DateTimeOffset StartsAtUtc,
    int DurationMinutes,
    string? UserAccessToken = null);

public sealed record ZoomMeetingResult(
    string MeetingId,
    string JoinUrl,
    string StartUrl);

public sealed record ZoomOAuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string? Email);

public sealed record ZoomConnectionStatus(
    bool Connected,
    string? Email,
    DateTimeOffset? ExpiresAt,
    bool AppFallbackAvailable);

public interface IZoomMeetingClient
{
    Task<ZoomMeetingResult> CreateMeetingAsync(ZoomMeetingRequest request, CancellationToken cancellationToken);
}

public interface IZoomUserOAuthService
{
    bool IsUserOAuthConfigured { get; }
    bool IsServerOAuthConfigured { get; }
    string FrontendRedirectUri { get; }
    string BuildAuthorizeUrl(Guid teacherUserId);
    Task<ZoomOAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<ZoomOAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    bool TryParseState(string state, out Guid teacherUserId);
}
