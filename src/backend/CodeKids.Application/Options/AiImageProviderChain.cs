namespace CodeKids.Application.Options;

/// <summary>
/// Bound to the "AiImage" config section — the multimodal (vision) provider chain,
/// e.g. Gemini. Used whenever images or files must be analyzed.
/// </summary>
public sealed class AiImageProviderChain : List<AiContentProviderOptions>
{
}
