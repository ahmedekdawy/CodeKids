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

    /// <summary>Folder name for a subject (course): its id, so renaming the course never moves its files.</summary>
    public static string SubjectFolder(Guid courseId) => courseId.ToString("D");

    /// <summary>
    /// Relative path for a new upload. With a subject folder: {tenant}/{subject}/{guid}{ext};
    /// otherwise {tenant}/yyyy/MM/dd/{guid}{ext}.
    /// </summary>
    public static string NewRelativePath(string? tenantId, string fileName, string contentType, string? subjectFolder = null)
    {
        var ext = Path.GetExtension(MediaFileTypes.EnsureFileName(fileName, contentType));
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 10 || !ext.Skip(1).All(char.IsAsciiLetterOrDigit))
        {
            ext = MediaFileTypes.ExtensionForContentType(contentType);
        }

        var folder = string.IsNullOrWhiteSpace(subjectFolder) ? $"{DateTime.UtcNow:yyyy/MM/dd}" : subjectFolder;
        return $"{TenantFolder(tenantId)}/{folder}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
    }
}
