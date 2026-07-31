namespace CodeKids.Infrastructure.Zoom;

public sealed class ZoomOptions
{
    public const string SectionName = "Zoom";

    public string AccountId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string HostUserId { get; set; } = "me";

    /// <summary>OAuth app credentials for teachers connecting personal Zoom accounts.</summary>
    public string UserOAuthClientId { get; set; } = string.Empty;
    public string UserOAuthClientSecret { get; set; } = string.Empty;
    public string UserOAuthRedirectUri { get; set; } = "http://localhost:5078/api/zoom/callback";
    public string FrontendRedirectUri { get; set; } = "http://localhost:4200/teacher/zoom";
    public string StateSigningKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsUserOAuthConfigured =>
        !string.IsNullOrWhiteSpace(UserOAuthClientId)
        && !string.IsNullOrWhiteSpace(UserOAuthClientSecret)
        && !string.IsNullOrWhiteSpace(UserOAuthRedirectUri);
}
