using UglyToad.PdfPig;

namespace CodeKids.Infrastructure.Ai;

/// <summary>
/// Extracts plain text from the course book (PDF learning material) so it can be
/// included in AI generation prompts.
/// </summary>
public static class PdfTextExtractor
{
    /// <summary>
    /// Reads up to <paramref name="maxPages"/> pages and returns roughly
    /// <paramref name="maxCharacters"/> of text, stopping early when the limit is reached.
    /// </summary>
    public static string Extract(Stream pdfStream, int maxPages = 60, int maxCharacters = 24000)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = new System.Text.StringBuilder();
        var pageLimit = Math.Min(document.NumberOfPages, maxPages);

        for (var page = 1; page <= pageLimit; page++)
        {
            var pageText = document.GetPage(page).Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            text.AppendLine(pageText.Trim());
            if (text.Length >= maxCharacters)
            {
                break;
            }
        }

        var result = text.ToString().Trim();
        return result.Length <= maxCharacters ? result : result[..maxCharacters];
    }
}
