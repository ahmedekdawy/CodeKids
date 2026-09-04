using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

        return string.Equals(studentAnswer.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutoGradable(BankQuestionType type) =>
        type is BankQuestionType.Choose
            or BankQuestionType.TrueFalse
            or BankQuestionType.SingleChoice
            or BankQuestionType.MultiChoice
            or BankQuestionType.Underline
            or BankQuestionType.ShortAnswer
            or BankQuestionType.FreeText;
}
