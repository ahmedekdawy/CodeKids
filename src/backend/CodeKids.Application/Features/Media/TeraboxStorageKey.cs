namespace CodeKids.Application.Features.Media;

public static class TeraboxStorageKey
{
    private const string Prefix = "terabox:";

    public static string Format(long fsId, string remotePath) => $"{Prefix}{fsId}|{remotePath}";

    public static bool IsTeraboxKey(string? storageKey) =>
        storageKey?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true;

    public static bool TryParse(string? storageKey, out long fsId, out string remotePath)
    {
        fsId = 0;
        remotePath = string.Empty;
        if (storageKey is null || !storageKey.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = storageKey[Prefix.Length..];
        var pipe = rest.IndexOf('|');
        if (pipe <= 0)
        {
            return false;
        }

        if (!long.TryParse(rest[..pipe], out fsId))
        {
            return false;
        }

        remotePath = rest[(pipe + 1)..];
        return !string.IsNullOrWhiteSpace(remotePath);
    }
}
