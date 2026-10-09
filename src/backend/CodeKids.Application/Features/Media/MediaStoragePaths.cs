namespace CodeKids.Application.Features.Media;

public static class MediaStoragePaths
{
    public const string SharedFolder = "shared";

    /// <summary>Folder name for a tenant's uploads; anything outside [A-Za-z0-9_-] is replaced.</summary>
    public static string TenantFolder(string? tenantId)
    {
        var chars = (tenantId ?? string.Empty).Trim()
            .Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? char.ToLowerInvariant(c) : '-')
            .ToArray();
        var folder = new string(chars).Trim('-');
        return folder.Length == 0 ? SharedFolder : folder;
    }

    /// <summary>Relative path for a new upload: {tenant}/yyyy/MM/dd/{guid}{ext}.</summary>
    public static string NewRelativePath(string? tenantId, string fileName, string contentType)
    {
        var ext = Path.GetExtension(MediaFileTypes.EnsureFileName(fileName, contentType));
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10 || !ext.Skip(1).All(char.IsAsciiLetterOrDigit))
        {
            ext = MediaFileTypes.ExtensionForContentType(contentType);
        }

        return $"{TenantFolder(tenantId)}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
    }
}
