using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodeKids.Application.Features.QuestionBank;

public static class CompleteBlanks
{
    public const string StudentPlaceholder = "{{blank}}";

    /// <summary>
    /// Blank answers are wrapped in a single # (e.g. #Cairo#).
    /// Legacy ##Cairo## is still accepted.
    /// </summary>
    private static readonly Regex BlankPattern = new(
        @"#+([^#]+)#+",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyList<string> Extract(string? markedText)
    {
        if (string.IsNullOrWhiteSpace(markedText))
        {
            return [];
        }

        return BlankPattern.Matches(markedText)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(value => value.Length > 0)
            .ToList();
    }

    public static IReadOnlyList<string> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed, JsonOptions);
                if (parsed is { Count: > 0 })
                {
                    return parsed.Select(x => (x ?? string.Empty).Trim()).Where(x => x.Length > 0).ToList();
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return Extract(trimmed);
    }

    public static string Join(IEnumerable<string> answers) =>
        JsonSerializer.Serialize(answers.Select(x => x.Trim()).Where(x => x.Length > 0).ToList(), JsonOptions);

    public static string Format(string? value)
    {
        var answers = Parse(value);
        return answers.Count == 0 ? (value ?? string.Empty).Trim() : string.Join(", ", answers);
    }

    public static string Mask(string? markedText) =>
        string.IsNullOrEmpty(markedText)
            ? string.Empty
            : BlankPattern.Replace(markedText, StudentPlaceholder);

    public static string PassageForClient(string questionType, string passageText, bool includeAnswerKey)
    {
        if (includeAnswerKey || !IsComplete(questionType))
        {
            return passageText ?? string.Empty;
        }

        return Mask(passageText);
    }

    public static bool IsComplete(string? questionType) =>
        string.Equals(questionType, nameof(Domain.Enums.BankQuestionType.Complete), StringComparison.OrdinalIgnoreCase);

    public static bool AnswersMatch(string? studentAnswer, string? correctAnswer)
    {
        var expected = Parse(correctAnswer);
        if (expected.Count == 0)
        {
            return false;
        }

        var actual = Parse(studentAnswer);
        if (actual.Count != expected.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(actual[i].Trim(), expected[i].Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
