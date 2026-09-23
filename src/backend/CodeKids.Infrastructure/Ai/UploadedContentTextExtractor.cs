using CodeKids.Application.Abstractions;

namespace CodeKids.Infrastructure.Ai;

/// <summary>
/// Infrastructure implementation over <see cref="PdfTextExtractor"/> (PdfPig).
/// </summary>
public sealed class UploadedContentTextExtractor : IUploadedContentTextExtractor
{
    public string TryExtractPdfText(Stream pdfStream, int maxCharacters, out string mimeType)
    {
        mimeType = "application/pdf";
        try
        {
            return PdfTextExtractor.Extract(pdfStream, maxCharacters: maxCharacters);
        }
        catch
        {
            return string.Empty;
        }
    }
}
