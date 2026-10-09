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

/// <summary>Result of a range read; RangeHandled is true when the returned content honors the requested byte range.</summary>
public sealed record StorageReadResult(
    Stream Content,
    bool RangeHandled,
    long? Start,
    long? End,
    long? TotalLength);

public interface IFileStorage
{
    /// <summary>Saves the file under the tenant's folder (see <see cref="Features.Media.MediaStoragePaths"/>).</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default, string? tenantId = null);

    /// <summary>Path of the stored file relative to the storage root, e.g. tenant/2026/10/09/name.mp4.</summary>
    string? GetRelativePath(string? storageKey);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    bool Exists(string storageKey);

    /// <summary>True when OpenReadAsync returns a seekable stream so ASP.NET can serve Range requests itself.</summary>
    bool SupportsRangeProcessing => false;

    /// <summary>Opens a byte range [start, end] of the file (end is inclusive; null means to the end of file).</summary>
    Task<StorageReadResult> OpenRangeAsync(string storageKey, long? start, long? end, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This storage provider does not support range reads.");
}

public interface ITeraboxDirectLinkResolver
{
    Task<string?> TryResolveAsync(string storageKey, CancellationToken cancellationToken = default);
}

public interface IMediaAccessTokenService
{
    string CreateToken(Guid mediaAssetId, Guid userId, TimeSpan lifetime, string? tenantId = null);
    bool TryValidate(string token, out Guid mediaAssetId, out Guid userId, out DateTimeOffset expiresAt, out string? tenantId);
}
