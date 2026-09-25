namespace CodeKids.Application.Abstractions;

public interface IStudyPlanAiClient
{
    Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        object? jsonSchema = null);

    /// <summary>
    /// Attachments (images or PDFs) sent inline to multimodal providers; ignored
    /// gracefully by text-only providers.
    /// </summary>
    public sealed record AiAttachment(string MimeType, byte[] Data);

    Task<string> CompleteJsonWithFilesAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<AiAttachment> attachments,
        CancellationToken cancellationToken,
        object? jsonSchema = null);
}
