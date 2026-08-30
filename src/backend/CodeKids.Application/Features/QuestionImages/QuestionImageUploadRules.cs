namespace CodeKids.Application.Features.QuestionImages;

public static class QuestionImageUploadRules
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public const long MaxPdfBytes = 20 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif",
        "application/pdf"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only PNG, JPEG, WebP, GIF images and PDF files are allowed.");
        }

        var maxBytes = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? MaxPdfBytes
            : MaxImageBytes;

        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new InvalidOperationException(
                contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    ? "PDF must be between 1 byte and 20 MB."
                    : "Image must be between 1 byte and 5 MB.");
        }
    }
}
