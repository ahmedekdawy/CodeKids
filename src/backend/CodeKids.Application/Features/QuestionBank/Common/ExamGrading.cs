using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.QuestionBank;

public static class ExamGrading
{
    public static IReadOnlyList<string> NormalizeMultiAnswer(string value) =>
        value
            .Split([',', ';', ' ', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Preserves sequence (Order questions). Drops empties; does not sort or dedupe.</summary>
    public static IReadOnlyList<string> ParseOrderedKeys(string value) =>
        value
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .Where(x => x.Length > 0)
            .ToList();

    public static string JoinOrderedKeys(IEnumerable<string> keys) =>
        string.Join(',', keys.Select(x => x.Trim().ToUpperInvariant()).Where(x => x.Length > 0));

    public static bool AnswersMatch(BankQuestionType type, string studentAnswer, string correctAnswer)
    {
        if (BankQuestionValidator.IsComposite(type))
        {
            return false;
        }

        if (BankQuestionValidator.IsTextAnswer(type))
        {
            // Text answers auto-grade only when a model answer was provided.
            return !string.IsNullOrWhiteSpace(correctAnswer)
                && string.Equals(studentAnswer.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        if (type == BankQuestionType.MultiChoice)
        {
            var left = NormalizeMultiAnswer(studentAnswer);
            var right = NormalizeMultiAnswer(correctAnswer);
            return left.Count == right.Count && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
        }

        if (type == BankQuestionType.Order)
        {
            var left = ParseOrderedKeys(studentAnswer);
            var right = ParseOrderedKeys(correctAnswer);
            return left.Count == right.Count && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
        }

        if (type == BankQuestionType.Map)
        {
            return MapMarkers.AnswersMatch(studentAnswer, correctAnswer);
        }

        if (type == BankQuestionType.Complete)
        {
            return CompleteBlanks.AnswersMatch(studentAnswer, correctAnswer);
        }

        return string.Equals(studentAnswer.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutoGradable(BankQuestionType type) =>
        type is BankQuestionType.Choose
            or BankQuestionType.TrueFalse
            or BankQuestionType.SingleChoice
            or BankQuestionType.MultiChoice
            or BankQuestionType.Order
            or BankQuestionType.Map
            or BankQuestionType.Underline
            or BankQuestionType.Complete
            or BankQuestionType.ShortAnswer
            or BankQuestionType.FreeText;
}
