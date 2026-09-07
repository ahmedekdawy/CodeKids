namespace CodeKids.Application.Features.Media;

public static class TeraboxDisplayUrl
{
    public const string DefaultBaseUrl = "https://www.1024terabox.com";

    public static string? BuildFromStorageKey(string? storageKey, string? configuredBaseUrl = null)
    {
        if (!TeraboxStorageKey.TryParse(storageKey, out _, out var remotePath))
        {
            return null;
        }

        var baseUrl = (configuredBaseUrl ?? DefaultBaseUrl).TrimEnd('/');
        var path = remotePath.StartsWith('/') ? remotePath : $"/{remotePath}";
        return $"{baseUrl}{path}";
    }

    public static string NormalizePlaybackUrl(string url, string? configuredBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var baseUrl = (configuredBaseUrl ?? DefaultBaseUrl).TrimEnd('/');
        var normalized = url;
        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            normalized = $"https:{normalized}";
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            return normalized;
        }

        return normalized.StartsWith('/') ? $"{baseUrl}{normalized}" : $"{baseUrl}/{normalized}";
    }

    public static bool IsTeraboxHost(string host) =>
        host.Contains("terabox", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("1024terabox", StringComparison.OrdinalIgnoreCase);
}
