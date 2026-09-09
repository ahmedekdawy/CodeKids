using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record MapMarkerDto(
    string Id,
    double X,
    double Y,
    string Label,
    string Kind);

public sealed record MapMarkerInput(
    string Id,
    double X,
    double Y,
    string? Label,
    string? Kind);

public static class MapMarkers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IReadOnlyList<MapMarkerDto> Normalize(IEnumerable<MapMarkerInput>? markers)
    {
        var list = new List<MapMarkerDto>();
        var index = 1;
        foreach (var marker in markers ?? [])
        {
            var kind = string.Equals(marker.Kind, "arrow", StringComparison.OrdinalIgnoreCase)
                ? "arrow"
                : "number";
            var id = string.IsNullOrWhiteSpace(marker.Id)
                ? index.ToString()
                : marker.Id.Trim();
            var label = kind == "arrow"
                ? string.Empty
                : (string.IsNullOrWhiteSpace(marker.Label) ? id : marker.Label.Trim());
            var x = Clamp(marker.X);
            var y = Clamp(marker.Y);
            list.Add(new MapMarkerDto(id, x, y, label, kind));
            index++;
        }

        return list;
    }

    public static string ToJson(IReadOnlyList<MapMarkerDto> markers) =>
        JsonSerializer.Serialize(markers, JsonOptions);

    public static IReadOnlyList<MapMarkerDto> Parse(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<MapMarkerDto>>(optionsJson, JsonOptions);
            if (parsed is null || parsed.Count == 0)
            {
                return [];
            }

            return Normalize(parsed.Select(m => new MapMarkerInput(m.Id, m.X, m.Y, m.Label, m.Kind)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static Dictionary<string, string> ParseAnswers(string? json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        var trimmed = json.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var value = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? string.Empty
                        : prop.Value.ToString();
                    result[prop.Name.Trim()] = value.Trim();
                }

                return result;
            }
            catch (JsonException)
            {
                // fall through to line format
            }
        }

        foreach (var part in trimmed.Split(['|', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var sep = part.IndexOf('=');
            if (sep <= 0)
            {
                sep = part.IndexOf(':');
            }

            if (sep <= 0)
            {
                continue;
            }

            var key = part[..sep].Trim();
            var value = part[(sep + 1)..].Trim();
            if (key.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    public static string JoinAnswers(IReadOnlyDictionary<string, string> answers) =>
        JsonSerializer.Serialize(
            answers.ToDictionary(x => x.Key, x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase),
            JsonOptions);

    public static bool AnswersMatch(string? studentAnswer, string? correctAnswer)
    {
        var left = ParseAnswers(studentAnswer);
        var right = ParseAnswers(correctAnswer);
        if (right.Count == 0)
        {
            return false;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, expected) in right)
        {
            if (!left.TryGetValue(key, out var actual))
            {
                return false;
            }

            if (!string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public static int CountMatches(string? studentAnswer, string? correctAnswer)
    {
        var left = ParseAnswers(studentAnswer);
        var right = ParseAnswers(correctAnswer);
        var matches = 0;
        foreach (var (key, expected) in right)
        {
            if (left.TryGetValue(key, out var actual)
                && string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                matches++;
            }
        }

        return matches;
    }

    private static double Clamp(double value) =>
        double.IsNaN(value) ? 50 : Math.Clamp(value, 0, 100);
}
