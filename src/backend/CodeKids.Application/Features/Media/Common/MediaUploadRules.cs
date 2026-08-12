using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public static class MediaUploadRules
{
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/quicktime"
    };

    public static void EnsureAllowed(string contentType, long sizeBytes, long maxBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only MP4, WebM, and MOV videos are allowed.");
        }

        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new InvalidOperationException($"File size must be between 1 byte and {maxBytes} bytes.");
        }
    }

    public static string NormalizeExternalUrl(string? url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Video URL is required.");
        }

        if (trimmed.Length > 1000)
        {
            throw new InvalidOperationException("Video URL is too long.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Video URL must be an absolute http or https link.");
        }

        return uri.AbsoluteUri;
    }
}
