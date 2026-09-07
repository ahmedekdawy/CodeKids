namespace CodeKids.Application.Features.Profile;

public static class ProfilePhotoUploadRules
{
    public const long MaxBytes = 3 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only PNG, JPEG and WebP photos are allowed.");
        }

        if (sizeBytes <= 0 || sizeBytes > MaxBytes)
        {
            throw new InvalidOperationException("Photo must be between 1 byte and 3 MB.");
        }
    }
}
