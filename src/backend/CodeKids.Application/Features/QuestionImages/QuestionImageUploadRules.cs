namespace CodeKids.Application.Features.QuestionImages;

public static class QuestionImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only PNG, JPEG, WebP, and GIF images are allowed.");
        }

        if (sizeBytes <= 0 || sizeBytes > MaxBytes)
        {
            throw new InvalidOperationException("Image must be between 1 byte and 5 MB.");
        }
    }
}
