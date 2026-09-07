namespace CodeKids.Application.Abstractions;

public sealed class MediaOptions
{
    public const string SectionName = "Media";
    /// <summary>Local or Terabox</summary>
    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "App_Data/media";
    public string SigningKey { get; set; } = "CodeKids-Media-Dev-Signing-Key-Change-Me";
    public int SignedUrlMinutes { get; set; } = 15;
    public long MaxUploadBytes { get; set; } = 500L * 1024 * 1024;
    /// <summary>Public API base used in signed playback URLs, e.g. https://api.example.com/api</summary>
    public string? PublicBaseUrl { get; set; }
}

public sealed class TeraboxOptions
{
    public const string SectionName = "Terabox";
    public string Ndus { get; set; } = string.Empty;
    public string JsToken { get; set; } = string.Empty;
    public string AppId { get; set; } = "250528";
    public string BdsToken { get; set; } = string.Empty;
    public string BrowserId { get; set; } = string.Empty;
    public string RemoteDirectory { get; set; } = "/CodeKids";
    public string BaseUrl { get; set; } = "https://www.1024terabox.com";

    /// <summary>Terabox Open Platform app key (apply at terabox.com/integrations).</summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>Terabox Open Platform app secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>Private secret used to sign OAuth token requests.</summary>
    public string PrivateSecret { get; set; } = string.Empty;
    /// <summary>Initial OAuth access token (optional; refreshed automatically).</summary>
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>OAuth refresh token from the initial authorization.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    bool Exists(string storageKey);
}

public interface ITeraboxDirectLinkResolver
{
    Task<string?> TryResolveAsync(string storageKey, CancellationToken cancellationToken = default);
}

public interface IMediaAccessTokenService
{
    string CreateToken(Guid mediaAssetId, Guid userId, TimeSpan lifetime);
    bool TryValidate(string token, out Guid mediaAssetId, out Guid userId, out DateTimeOffset expiresAt);
}
