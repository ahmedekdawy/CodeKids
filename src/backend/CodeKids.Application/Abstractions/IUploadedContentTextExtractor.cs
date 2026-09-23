namespace CodeKids.Application.Abstractions;

/// <summary>
/// Extracts text content from an uploaded PDF or image so it can be included in
/// AI prompts (used by the Smart Study Assistant). Images are not OCR'd; callers
/// must pass the raw bytes to multimodal models separately when needed.
/// </summary>
public interface IUploadedContentTextExtractor
{
    /// <summary>
    /// Extracts plain text from a PDF stream. Returns an empty string for
    /// non-PDF content (e.g. images) or when extraction fails.
    /// </summary>
    string TryExtractPdfText(Stream pdfStream, int maxCharacters, out string mimeType);
}
