namespace CodeKids.Application.Abstractions;

public sealed class MediaOptions
{
    public const string SectionName = "Media";
    public string RootPath { get; set; } = "App_Data/media";
    public string SigningKey { get; set; } = "CodeKids-Media-Dev-Signing-Key-Change-Me";
    public int SignedUrlMinutes { get; set; } = 15;
    public long MaxUploadBytes { get; set; } = 500L * 1024 * 1024;
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    bool Exists(string storageKey);
}

public interface IMediaAccessTokenService
{
    string CreateToken(Guid mediaAssetId, Guid userId, TimeSpan lifetime);
    bool TryValidate(string token, out Guid mediaAssetId, out Guid userId, out DateTimeOffset expiresAt);
}
