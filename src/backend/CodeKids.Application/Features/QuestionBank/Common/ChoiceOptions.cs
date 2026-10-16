using System.Text.Json;

namespace CodeKids.Application.Features.QuestionBank;

public static class ChoiceOptions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<ChoiceOptionDto> FromTexts(IEnumerable<string>? texts)
    {
        var list = new List<ChoiceOptionDto>();
        var index = 0;
        foreach (var text in texts ?? [])
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (index >= 26)
            {
                break;
            }

            list.Add(new ChoiceOptionDto(((char)('A' + index)).ToString(), text.Trim()));
            index++;
        }

        return list;
    }

    public static string ToJson(IReadOnlyList<ChoiceOptionDto> options) =>
        JsonSerializer.Serialize(options, JsonOptions);

    public static IReadOnlyList<ChoiceOptionDto> Parse(
        string? optionsJson,
        string? optionA = null,
        string? optionB = null,
        string? optionC = null,
        string? optionD = null)
    {
        if (!string.IsNullOrWhiteSpace(optionsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<ChoiceOptionDto>>(optionsJson, JsonOptions);
                if (parsed is { Count: > 0 })
                {
                    return parsed
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Text))
                        .Select((x, i) => new ChoiceOptionDto(
                            string.IsNullOrWhiteSpace(x.Key) ? ((char)('A' + i)).ToString() : x.Key.Trim().ToUpperInvariant(),
                            x.Text.Trim()))
                        .ToList();
                }
            }
            catch (JsonException)
            {
                // fall back to legacy columns
            }
        }

        return FromTexts([optionA ?? string.Empty, optionB ?? string.Empty, optionC ?? string.Empty, optionD ?? string.Empty]);
    }

    /// <summary>Length of the old OptionA–D columns. The full option text lives in OptionsJson.</summary>
    public const int LegacyColumnLength = 200;

    public static (string? A, string? B, string? C, string? D) ToLegacy(IReadOnlyList<ChoiceOptionDto> options) =>
    (
        FitLegacy(options.ElementAtOrDefault(0)?.Text),
        FitLegacy(options.ElementAtOrDefault(1)?.Text),
        FitLegacy(options.ElementAtOrDefault(2)?.Text),
        FitLegacy(options.ElementAtOrDefault(3)?.Text)
    );

    private static string? FitLegacy(string? text) =>
        text is { Length: > LegacyColumnLength } ? text[..LegacyColumnLength] : text;

    public static HashSet<string> AllowedKeys(IReadOnlyList<ChoiceOptionDto> options) =>
        options.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Turns stored keys such as "A" or "A,C" into "A) option text".</summary>
    public static string FormatAnswer(IReadOnlyList<ChoiceOptionDto> options, string? keys, bool preserveOrder = false)
    {
        if (string.IsNullOrWhiteSpace(keys))
        {
            return string.Empty;
        }

        if (options.Count == 0)
        {
            return keys.Trim();
        }

        var parts = preserveOrder
            ? ExamGrading.ParseOrderedKeys(keys)
            : ExamGrading.NormalizeMultiAnswer(keys);
        return string.Join(", ", parts.Select(part =>
        {
            var match = options.FirstOrDefault(o => string.Equals(o.Key, part, StringComparison.OrdinalIgnoreCase));
            return match is null ? part : $"{match.Key}) {match.Text}";
        }));
    }
}
