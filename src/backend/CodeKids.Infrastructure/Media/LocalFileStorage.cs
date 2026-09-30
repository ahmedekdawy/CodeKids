using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;
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

    public bool SupportsRangeProcessing => true;

    public Task<StorageReadResult> OpenRangeAsync(string storageKey, long? start, long? end, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Media file not found.", storageKey);
        }

        var stream = File.OpenRead(fullPath);
        var total = stream.Length;
        if (start is long from)
        {
            stream.Seek(from, SeekOrigin.Begin);
        }

        var effectiveEnd = Math.Min(end ?? total - 1, total - 1);
        Stream content = end is long e && e < total - 1
            ? new PartialReadStream(stream, effectiveEnd - (start ?? 0) + 1)
            : stream;

        return Task.FromResult(new StorageReadResult(content, true, start ?? 0, effectiveEnd, total));
    }

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
        MediaFileTypes.ExtensionForContentType(contentType);
}

/// <summary>A read-only wrapper stream limited to the first <paramref name="length"/> bytes of an underlying stream.</summary>
public sealed class PartialReadStream(Stream inner, long length) : Stream
{
    private long _remaining = length;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_remaining <= 0) return 0;
        var read = inner.Read(buffer, offset, (int)Math.Min(count, _remaining));
        _remaining -= read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0) return 0;
        var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _remaining)], cancellationToken);
        _remaining -= read;
        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

public sealed class MediaAccessTokenService(IOptions<MediaOptions> options) : IMediaAccessTokenService
{
    public string CreateToken(Guid mediaAssetId, Guid userId, TimeSpan lifetime, string? tenantId = null)
    {
        var expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var payload = $"{mediaAssetId:N}.{userId:N}.{expires}";
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            payload += $".{Uri.EscapeDataString(tenantId)}";
        }

        var sig = Sign(payload);
        return $"{payload}.{sig}";
    }

    public bool TryValidate(string token, out Guid mediaAssetId, out Guid userId, out DateTimeOffset expiresAt, out string? tenantId)
    {
        mediaAssetId = Guid.Empty;
        userId = Guid.Empty;
        expiresAt = default;
        tenantId = null;

        var parts = (token ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (4 or 5))
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[0], "N", out mediaAssetId)
            || !Guid.TryParseExact(parts[1], "N", out userId)
            || !long.TryParse(parts[2], out var expiresUnix))
        {
            return false;
        }

        if (parts.Length == 5)
        {
            tenantId = Uri.UnescapeDataString(parts[3]);
        }

        var payload = string.Join('.', parts.Take(parts.Length - 1));
        var expected = Sign(payload);
        var signature = parts[^1];

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature)))
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
