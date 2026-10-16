using System.Net;
using System.Text.RegularExpressions;

namespace CodeKids.Application.Common;

/// <summary>Turns editor HTML into the text a reader sees, so formatted and plain answers compare equal.</summary>
public static partial class RichText
{
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        if (!html.Contains('<') && !html.Contains('&'))
        {
            return Whitespace().Replace(html, " ").Trim();
        }

        var text = LineBreaks().Replace(html, " ");
        text = Tags().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        return Whitespace().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"<\s*br\s*/?\s*>|</\s*(p|div|li)\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreaks();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
