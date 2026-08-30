using System.Text.Json;

namespace CodeKids.Application.Features.Classrooms;

public sealed record ClassroomZoomLinkDto(string Name, string Url);

public static class ClassroomZoomLinks
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<ClassroomZoomLinkDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ClassroomZoomLinkDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Serialize(IReadOnlyList<ClassroomZoomLinkDto>? links)
    {
        var normalized = Normalize(links);
        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    public static IReadOnlyList<ClassroomZoomLinkDto> Normalize(IReadOnlyList<ClassroomZoomLinkDto>? links)
    {
        if (links is null || links.Count == 0)
        {
            return [];
        }

        var result = new List<ClassroomZoomLinkDto>();
        foreach (var link in links)
        {
            var name = (link.Name ?? string.Empty).Trim();
            var url = (link.Url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"Zoom link \"{name}\" must be a valid http or https URL.");
            }

            result.Add(new ClassroomZoomLinkDto(name, url));
        }

        return result;
    }
}
