namespace CodeKids.Application.Features.Media;

/// <summary>
/// Extracts the YouTube video key (e.g. "V9mTgsEvkt4") that a video URL and its live chat URL share,
/// so the same key builds both the embedded video and the student's chat panel.
/// </summary>
public static class YouTubeVideoKey
{
    /// <summary>Returns the key for a youtube.com / youtu.be link, or null when the URL is not a YouTube video.</summary>
    public static string? TryExtract(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        // Covers youtube.com, m./music./studio. subdomains, youtu.be and youtube-nocookie.com.
        var isYouTubeHost =
            host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal) ||
            host == "youtu.be" || host.EndsWith(".youtu.be", StringComparison.Ordinal) ||
            host == "youtube-nocookie.com" || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal);
        if (!isYouTubeHost)
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? candidate = null;

        if (host.EndsWith("youtu.be", StringComparison.Ordinal))
        {
            candidate = segments.Length > 0 ? segments[0] : null;
        }
        else if (segments.Length > 0)
        {
            switch (segments[0].ToLowerInvariant())
            {
                // studio.youtube.com/video/<key>/livestreaming
                case "video" when segments.Length > 1:
                    candidate = segments[1];
                    break;
                // youtube.com/embed/<key>, /live/<key>, /shorts/<key>, /v/<key>
                case "embed" or "live" or "shorts" or "v" when segments.Length > 1:
                    candidate = segments[1];
                    break;
                // youtube.com/watch?v=<key>
                case "watch":
                    candidate = QueryParam(uri, "v");
                    break;
                default:
                    candidate = QueryParam(uri, "v");
                    break;
            }
        }

        candidate ??= QueryParam(uri, "v");
        return IsValidKey(candidate) ? candidate : null;
    }

    private static bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length is < 6 or > 20)
        {
            return false;
        }

        foreach (var ch in key)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_')
            {
                return false;
            }
        }

        return true;
    }

    private static string? QueryParam(Uri uri, string name)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            if (!string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return separator < 0 ? string.Empty : Uri.UnescapeDataString(pair[(separator + 1)..]);
        }

        return null;
    }
}
