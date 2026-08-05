using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Media;

public sealed class LocalFileStorage(IOptions<MediaOptions> options) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
        {
            ext = GuessExtension(contentType);
        }

        var storageKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Media file not found.", storageKey);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public bool Exists(string storageKey) => File.Exists(ResolvePath(storageKey));

    private string ResolvePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return fullPath;
    }

    private static string GuessExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "video/quicktime" => ".mov",
            _ => ".bin"
        };
}

public sealed class MediaAccessTokenService(IOptions<MediaOptions> options) : IMediaAccessTokenService
{
    public string CreateToken(Guid mediaAssetId, Guid userId, TimeSpan lifetime)
    {
        var expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var payload = $"{mediaAssetId:N}.{userId:N}.{expires}";
        var sig = Sign(payload);
        return $"{payload}.{sig}";
    }

    public bool TryValidate(string token, out Guid mediaAssetId, out Guid userId, out DateTimeOffset expiresAt)
    {
        mediaAssetId = Guid.Empty;
        userId = Guid.Empty;
        expiresAt = default;

        var parts = (token ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[0], "N", out mediaAssetId)
            || !Guid.TryParseExact(parts[1], "N", out userId)
            || !long.TryParse(parts[2], out var expiresUnix))
        {
            return false;
        }

        var payload = $"{parts[0]}.{parts[1]}.{parts[2]}";
        var expected = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[3])))
        {
            return false;
        }

        expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
        return expiresAt >= DateTimeOffset.UtcNow;
    }

    private string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.SigningKey);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
