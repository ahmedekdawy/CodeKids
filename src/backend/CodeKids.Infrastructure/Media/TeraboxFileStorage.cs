using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Media;

public sealed class TeraboxFileStorage(
    TeraboxClient teraboxClient,
    IOptions<TeraboxOptions> teraboxOptions) : IFileStorage
{
    private readonly TeraboxOptions _options = teraboxOptions.Value;

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeName = MediaFileTypes.EnsureFileName(fileName, contentType);
        var ext = Path.GetExtension(safeName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10)
        {
            ext = MediaFileTypes.ExtensionForContentType(contentType);
        }

        var remoteName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var remoteDirectory = $"{NormalizeDirectory(_options.RemoteDirectory)}/{DateTime.UtcNow:yyyy/MM/dd}";

        var tempPath = Path.Combine(Path.GetTempPath(), $"codekids-{Guid.NewGuid():N}{ext}");
        try
        {
            await using (var temp = File.Create(tempPath))
            {
                await content.CopyToAsync(temp, cancellationToken);
            }

            var result = await teraboxClient.UploadAsync(tempPath, remoteDirectory, remoteName, cancellationToken);
            return TeraboxStorageKey.Format(result.FsId, result.RemotePath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TeraboxStorageKey.TryParse(storageKey, out var fsId, out var remotePath))
        {
            throw new InvalidOperationException("Invalid Terabox storage key.");
        }

        return await teraboxClient.OpenReadAsync(fsId, remotePath, cancellationToken);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TeraboxStorageKey.TryParse(storageKey, out _, out var remotePath))
        {
            return;
        }

        await teraboxClient.DeleteAsync(remotePath, cancellationToken);
    }

    public bool Exists(string storageKey)
    {
        if (!TeraboxStorageKey.TryParse(storageKey, out _, out var remotePath))
        {
            return false;
        }

        return teraboxClient.ExistsAsync(remotePath).GetAwaiter().GetResult();
    }

    private static string NormalizeDirectory(string directory)
    {
        var normalized = (directory ?? "/").Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "/";
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.TrimEnd('/');
    }
}
