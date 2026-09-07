using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public static class BankQuestionValidator
{
    public static bool IsComposite(BankQuestionType type) =>
        type is BankQuestionType.Paragraph;

    public static bool IsFreeText(BankQuestionType type) =>
        type is BankQuestionType.FreeText;

    public static bool IsShortAnswer(BankQuestionType type) =>
        type is BankQuestionType.ShortAnswer;

    public static bool IsTextAnswer(BankQuestionType type) =>
        IsFreeText(type) || IsShortAnswer(type);

    public static BankQuestionType ParseType(string value)
    {
        if (string.Equals(value, "MultipleChoice", StringComparison.OrdinalIgnoreCase))
        {
            return BankQuestionType.SingleChoice;
        }

        if (!Enum.TryParse<BankQuestionType>(value, true, out var type) || !Enum.IsDefined(type))
        {
            throw new InvalidOperationException(
                "Question type must be Choose, TrueFalse, SingleChoice, MultiChoice, Paragraph, Underline, FreeText, or ShortAnswer.");
        }

        return type;
    }

    public static void ValidateLeaf(
        BankQuestionType type,
        string prompt,
        string? optionA,
        string? optionB,
        string? optionC,
        string? optionD,
        string correctAnswer,
        string? passageText = null,
        IReadOnlyList<string>? options = null)
    {
        if (string.IsNullOrWhiteSpace(StripHtml(prompt)))
        {
            throw new InvalidOperationException("Question prompt is required.");
        }

        if (IsComposite(type))
        {
            return;
        }

        if (IsTextAnswer(type))
        {
            return;
        }

        if (type == BankQuestionType.Underline)
        {
            if (string.IsNullOrWhiteSpace(passageText))
            {
                throw new InvalidOperationException("Underline questions require the sentence/text to underline in.");
            }

            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                throw new InvalidOperationException("Underline questions require the correct underlined phrase.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(correctAnswer))
        {
            throw new InvalidOperationException("Correct answer is required.");
        }

        if (type == BankQuestionType.TrueFalse)
        {
            if (!IsTrueFalse(correctAnswer))
            {
                throw new InvalidOperationException("True/False correct answer must be True or False.");
            }

            return;
        }

        if (type is BankQuestionType.Choose or BankQuestionType.SingleChoice or BankQuestionType.MultiChoice)
        {
            var choiceOptions = options is { Count: > 0 }
                ? ChoiceOptions.FromTexts(options)
                : ChoiceOptions.Parse(null, optionA, optionB, optionC, optionD);

            if (choiceOptions.Count < 2)
            {
                throw new InvalidOperationException("At least two answer options are required.");
            }

            var allowed = ChoiceOptions.AllowedKeys(choiceOptions);
            var keys = ExamGrading.NormalizeMultiAnswer(correctAnswer);
            if (keys.Count == 0)
            {
                throw new InvalidOperationException("Select a correct answer from the options list.");
            }

            if (type is BankQuestionType.Choose or BankQuestionType.SingleChoice)
            {
                if (keys.Count != 1 || !allowed.Contains(keys[0]))
                {
                    throw new InvalidOperationException("Correct answer must be one of the listed options.");
                }
            }
            else if (keys.Any(k => !allowed.Contains(k)))
            {
                throw new InvalidOperationException("MultiChoice correct answers must be among the listed options.");
            }
        }
    }

    public static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var withoutTags = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static bool IsTrueFalse(string value) =>
        string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "False", StringComparison.OrdinalIgnoreCase);
}
