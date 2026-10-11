using System.Text.RegularExpressions;

namespace CodeKids.Application.Features.Tenants;

/// <summary>IANA time zone ids as sent by the browser (e.g. Africa/Cairo). The API only stores them.</summary>
public static partial class TimeZoneIds
{
    public const int MaxLength = 64;

    /// <summary>Returns the trimmed id, or null when blank. Throws when it does not look like a zone id.</summary>
    public static string? Normalize(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return null;
        }

        if (value.Length > MaxLength || !Pattern().IsMatch(value))
        {
            throw new InvalidOperationException("Time zone is not valid.");
        }

        return value;
    }

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_+-]*(/[A-Za-z0-9_+-]+)*$")]
    private static partial Regex Pattern();
}
