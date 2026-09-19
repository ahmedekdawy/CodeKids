using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.LearningMaterials;

public static class LearningMaterialUploadRules
{
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "audio/mpeg",
        "audio/mp3",
        "audio/wav",
        "audio/x-wav",
        "audio/wave",
        "audio/ogg",
        "audio/webm",
        "audio/mp4",
        "audio/aac",
        "audio/x-m4a",
        "audio/m4a"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes, long maxBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only PDF, image, and audio files are allowed.");
        }

        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new InvalidOperationException($"File size must be between 1 byte and {maxBytes} bytes.");
        }
    }

    public static LearningMaterialKind KindFromContentType(string? contentType)
    {
        var normalized = Media.MediaFileTypes.NormalizeContentType(contentType);
        if (normalized == "application/pdf")
        {
            return LearningMaterialKind.Pdf;
        }

        if (normalized.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return LearningMaterialKind.Audio;
        }

        if (normalized.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return LearningMaterialKind.Image;
        }

        throw new InvalidOperationException("Only PDF, image, and audio files are allowed.");
    }
}
