using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.QuestionBank;

public static class TypedQuestionSupport
{
    public const string AssignmentTypeError =
        "Question type must be ShortAnswer, MultipleChoice, Choose, TrueFalse, SingleChoice, MultiChoice, Paragraph, Underline, or FreeText.";

    public static AssignmentQuestionType ParseAssignmentType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<AssignmentQuestionType>(value, true, out var type)
            || !Enum.IsDefined(type))
        {
            throw new InvalidOperationException(AssignmentTypeError);
        }

        return type;
    }

    public static BankQuestionType ParseQuizType(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? BankQuestionType.SingleChoice
            : BankQuestionValidator.ParseType(value);

    public static bool IsShortAnswer(AssignmentQuestionType type) =>
        type == AssignmentQuestionType.ShortAnswer;

    public static bool IsFreeText(AssignmentQuestionType type) =>
        type == AssignmentQuestionType.FreeText;

    public static bool IsTeacherGradedText(AssignmentQuestionType type) =>
        IsShortAnswer(type) || IsFreeText(type);

    public static bool IsComposite(AssignmentQuestionType type) =>
        type == AssignmentQuestionType.Paragraph;

    public static BankQuestionType ToBankType(AssignmentQuestionType type) => type switch
    {
        AssignmentQuestionType.MultipleChoice => BankQuestionType.SingleChoice,
        AssignmentQuestionType.Choose => BankQuestionType.Choose,
        AssignmentQuestionType.TrueFalse => BankQuestionType.TrueFalse,
        AssignmentQuestionType.SingleChoice => BankQuestionType.SingleChoice,
        AssignmentQuestionType.MultiChoice => BankQuestionType.MultiChoice,
        AssignmentQuestionType.Paragraph => BankQuestionType.Paragraph,
        AssignmentQuestionType.Underline => BankQuestionType.Underline,
        AssignmentQuestionType.FreeText => BankQuestionType.FreeText,
        _ => throw new InvalidOperationException("ShortAnswer is not a bank question type.")
    };

    public static IReadOnlyList<ChoiceOptionDto> ResolveOptions(
        BankQuestionType type,
        IReadOnlyList<string>? options,
        string? optionA,
        string? optionB,
        string? optionC,
        string? optionD = null)
    {
        if (type is not (BankQuestionType.Choose or BankQuestionType.SingleChoice or BankQuestionType.MultiChoice))
        {
            return [];
        }

        return options is { Count: > 0 }
            ? ChoiceOptions.FromTexts(options)
            : ChoiceOptions.Parse(null, optionA, optionB, optionC, optionD);
    }

    public static string NormalizeCorrect(BankQuestionType type, string? correct) =>
        type == BankQuestionType.MultiChoice
            ? string.Join(',', ExamGrading.NormalizeMultiAnswer(correct ?? string.Empty))
            : type == BankQuestionType.FreeText
                ? string.Empty
                : (correct ?? string.Empty).Trim();

    public static void ValidateAssignment(
        AssignmentQuestionType type,
        string prompt,
        string? optionA,
        string? optionB,
        string? optionC,
        string correctAnswer,
        string? passageText,
        IReadOnlyList<string>? options,
        IReadOnlyList<AssignmentChildSpec>? children)
    {
        if (IsTeacherGradedText(type))
        {
            if (string.IsNullOrWhiteSpace(BankQuestionValidator.StripHtml(prompt)))
            {
                throw new InvalidOperationException("Question prompt is required.");
            }

            if (children is { Count: > 0 })
            {
                throw new InvalidOperationException("Only Paragraph questions can have child questions.");
            }

            return;
        }

        var bankType = ToBankType(type);
        BankQuestionValidator.ValidateLeaf(
            bankType,
            prompt,
            optionA,
            optionB,
            optionC,
            null,
            correctAnswer ?? string.Empty,
            passageText,
            options);

        ValidateChildren(bankType, passageText, children, allowShortAnswerChildren: true);
    }

    public static void ValidateQuiz(
        BankQuestionType type,
        string prompt,
        string? optionA,
        string? optionB,
        string? optionC,
        string correctAnswer,
        string? passageText,
        IReadOnlyList<string>? options,
        IReadOnlyList<QuizChildSpec>? children)
    {
        if (BankQuestionValidator.IsFreeText(type))
        {
            throw new InvalidOperationException("FreeText questions are not supported in quizzes.");
        }

        BankQuestionValidator.ValidateLeaf(
            type,
            prompt,
            optionA,
            optionB,
            optionC,
            null,
            correctAnswer ?? string.Empty,
            passageText,
            options);

        ValidateChildren(
            type,
            passageText,
            children?.Select(c => new AssignmentChildSpec(
                c.Prompt,
                c.QuestionType,
                c.OptionA,
                c.OptionB,
                c.OptionC,
                c.Options,
                c.CorrectAnswer,
                c.Points,
                c.SortOrder,
                c.PromptImageMediaAssetId,
                c.Id)).ToList(),
            allowShortAnswerChildren: false,
            allowFreeTextChildren: false);
    }

    public static IReadOnlyList<Guid> FlattenIds<T>(IEnumerable<T> items, Func<T, Guid?> idSelector, Func<T, IEnumerable<T>?> childrenSelector)
    {
        var ids = new List<Guid>();
        foreach (var item in items)
        {
            if (idSelector(item) is Guid id)
            {
                ids.Add(id);
            }

            var children = childrenSelector(item);
            if (children is not null)
            {
                ids.AddRange(FlattenIds(children, idSelector, childrenSelector));
            }
        }

        return ids;
    }

    public static async Task EnsureImageAsync(IAppDbContext dbContext, Guid? imageId, CancellationToken cancellationToken) =>
        await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, imageId, cancellationToken);

    private static void ValidateChildren(
        BankQuestionType parentType,
        string? passageText,
        IReadOnlyList<AssignmentChildSpec>? children,
        bool allowShortAnswerChildren,
        bool allowFreeTextChildren = true)
    {
        if (BankQuestionValidator.IsComposite(parentType))
        {
            if (string.IsNullOrWhiteSpace(passageText))
            {
                throw new InvalidOperationException("Paragraph questions require passage text.");
            }

            if (children is null || children.Count == 0)
            {
                throw new InvalidOperationException("Paragraph questions need at least one child question.");
            }

            foreach (var child in children)
            {
                if (IsShortAnswerChild(child.QuestionType))
                {
                    if (!allowShortAnswerChildren)
                    {
                        throw new InvalidOperationException("ShortAnswer child questions are only allowed in assignments.");
                    }

                    if (string.IsNullOrWhiteSpace(BankQuestionValidator.StripHtml(child.Prompt)))
                    {
                        throw new InvalidOperationException("Question prompt is required.");
                    }

                    continue;
                }

                var childType = BankQuestionValidator.ParseType(child.QuestionType);
                if (BankQuestionValidator.IsComposite(childType) || childType == BankQuestionType.Underline)
                {
                    throw new InvalidOperationException("Child questions cannot be Paragraph or Underline.");
                }

                if (BankQuestionValidator.IsFreeText(childType) && !allowFreeTextChildren)
                {
                    throw new InvalidOperationException("FreeText questions are not supported in quizzes.");
                }

                BankQuestionValidator.ValidateLeaf(
                    childType,
                    child.Prompt,
                    child.OptionA,
                    child.OptionB,
                    child.OptionC,
                    null,
                    child.CorrectAnswer,
                    options: child.Options);
            }

            return;
        }

        if (children is { Count: > 0 })
        {
            throw new InvalidOperationException("Only Paragraph questions can have child questions.");
        }
    }

    private static bool IsShortAnswerChild(string? questionType) =>
        string.Equals(questionType, nameof(AssignmentQuestionType.ShortAnswer), StringComparison.OrdinalIgnoreCase);
}

public sealed record AssignmentChildSpec(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    Guid? Id = null);

public sealed record QuizChildSpec(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    Guid? PromptImageMediaAssetId,
    Guid? Id = null);
