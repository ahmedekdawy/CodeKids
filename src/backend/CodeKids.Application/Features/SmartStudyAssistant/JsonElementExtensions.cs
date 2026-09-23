using System.Text.Json;

namespace CodeKids.Application.Features.SmartStudyAssistant;

internal static class JsonElementExtensions
{
    public static readonly JsonElement EmptyArray = JsonSerializer.SerializeToElement(Array.Empty<object>());
}
