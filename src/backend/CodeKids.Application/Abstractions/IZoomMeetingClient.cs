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
    bool AppFallbackAvailable,
    bool UserOAuthConfigured = false,
    string? UserOAuthRedirectUri = null,
    string? UserOAuthClientIdMasked = null);

public sealed record ZoomUserOAuthSettingsDto(
    bool Configured,
    string ClientId,
    string ClientSecretMasked,
    bool HasClientSecret,
    string RedirectUri,
    string FrontendRedirectUri,
    string SuggestedRedirectUri);

public sealed record SaveZoomUserOAuthSettingsRequest(
    string ClientId,
    string? ClientSecret,
    string? RedirectUri,
    string? FrontendRedirectUri);

public interface IZoomMeetingClient
{
    Task<ZoomMeetingResult> CreateMeetingAsync(ZoomMeetingRequest request, CancellationToken cancellationToken);
}

public interface IZoomOAuthSettingsStore
{
    bool IsUserOAuthConfigured { get; }
    ZoomUserOAuthSettingsSnapshot Get();
    void Save(string clientId, string? clientSecret, string? redirectUri, string? frontendRedirectUri);
}

public sealed record ZoomUserOAuthSettingsSnapshot(
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string FrontendRedirectUri);

public interface IZoomUserOAuthService
{
    bool IsUserOAuthConfigured { get; }
    bool IsServerOAuthConfigured { get; }
    string FrontendRedirectUri { get; }
    string UserOAuthRedirectUri { get; }
    string? MaskedClientId { get; }
    string BuildAuthorizeUrl(Guid teacherUserId);
    Task<ZoomOAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<ZoomOAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    bool TryParseState(string state, out Guid teacherUserId);
}
