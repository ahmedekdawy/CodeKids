namespace CodeKids.Application.Features.Media;

public static class MediaFileTypes
{
    public static string NormalizeContentType(string? contentType)
    {
        var normalized = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "image/jpg" => "image/jpeg",
            "image/pjpeg" => "image/jpeg",
            "" => "application/octet-stream",
            _ => normalized
        };
    }

    public static string ExtensionForContentType(string? contentType)
    {
        return NormalizeContentType(contentType) switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "application/pdf" => ".pdf",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "video/quicktime" => ".mov",
            _ => ".bin"
        };
    }

    public static string ContentTypeForExtension(string? extension)
    {
        var ext = (extension ?? string.Empty).Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
        {
            ext = $".{ext}";
        }

        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }

    public static string EnsureFileName(string? fileName, string contentType)
    {
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "upload" : Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "upload";
        }

        if (!Path.HasExtension(safeName))
        {
            safeName += ExtensionForContentType(contentType);
        }

        return safeName;
    }

    public static string ResolveContentType(string? storedContentType, string? storageKey)
    {
        var normalized = NormalizeContentType(storedContentType);
        if (normalized is "application/octet-stream" or "binary/octet-stream")
        {
            var fromExt = ContentTypeForExtension(Path.GetExtension(storageKey ?? string.Empty));
            if (fromExt != "application/octet-stream")
            {
                return fromExt;
            }
        }

        return normalized;
    }
}
