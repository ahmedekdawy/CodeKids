using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record BankChildQuestionInput(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string CorrectAnswer,
    int Points,
    int SortOrder);

public sealed record CreateBankQuestionRequest(
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    IReadOnlyList<BankChildQuestionInput>? Children);

public sealed record CreateBankQuestionCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? LessonId,
    string QuestionType,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder,
    IReadOnlyList<BankChildQuestionInput>? Children) : ICommand<BankQuestionDto>;

public sealed record UpdateBankQuestionRequest(
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder);

public sealed record UpdateBankQuestionCommand(
    Guid TeacherUserId,
    Guid QuestionId,
    Guid? LessonId,
    string Prompt,
    string? PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int SortOrder) : ICommand<BankQuestionDto>;

public sealed record DeleteBankQuestionCommand(Guid TeacherUserId, Guid QuestionId) : ICommand<bool>;

public sealed record ListBankQuestionsQuery(Guid TeacherUserId, Guid? CourseId = null, Guid? LessonId = null)
    : IQuery<IReadOnlyList<BankQuestionDto>>;

public sealed record BankQuestionDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid? LessonId,
    string? LessonTitle,
    Guid CreatedByUserId,
    Guid? ParentQuestionId,
    string QuestionType,
    string Prompt,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<ChoiceOptionDto> Options,
    string CorrectAnswer,
    int Points,
    int SortOrder,
    IReadOnlyList<BankQuestionDto> Children);

public static class BankQuestionValidator
{
    public static bool IsComposite(BankQuestionType type) =>
        type is BankQuestionType.Paragraph;

    public static BankQuestionType ParseType(string value)
    {
        if (!Enum.TryParse<BankQuestionType>(value, true, out var type))
        {
            throw new InvalidOperationException(
                "Question type must be Choose, TrueFalse, SingleChoice, MultiChoice, Paragraph, or Underline.");
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
            or BankQuestionType.Underline;
}

public sealed class CreateBankQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateBankQuestionCommand, BankQuestionDto>
{
    public async Task<BankQuestionDto> Handle(CreateBankQuestionCommand command, CancellationToken cancellationToken)
    {
        await EnsureTeacherCanUseCourse(dbContext, command.TeacherUserId, command.CourseId, cancellationToken);

        if (command.LessonId is Guid lessonId)
        {
            var lessonOk = await dbContext.Lessons.AnyAsync(
                x => x.Id == lessonId && x.CourseId == command.CourseId, cancellationToken);
            if (!lessonOk)
            {
                throw new InvalidOperationException("Lesson not found for this course.");
            }
        }

        var type = BankQuestionValidator.ParseType(command.QuestionType);
        BankQuestionValidator.ValidateLeaf(
            type,
            command.Prompt,
            command.OptionA,
            command.OptionB,
            command.OptionC,
            command.OptionD,
            command.CorrectAnswer ?? string.Empty,
            command.PassageText,
            command.Options);

        if (BankQuestionValidator.IsComposite(type))
        {
            if (string.IsNullOrWhiteSpace(command.PassageText))
            {
                throw new InvalidOperationException("Paragraph questions require passage text.");
            }

            if (command.Children is null || command.Children.Count == 0)
            {
                throw new InvalidOperationException("Paragraph questions need at least one child question.");
            }
        }
        else if (command.Children is { Count: > 0 })
        {
            throw new InvalidOperationException("Only Paragraph questions can have child questions.");
        }

        var rootOptions = ResolveOptions(type, command.Options, command.OptionA, command.OptionB, command.OptionC, command.OptionD);
        var (legacyA, legacyB, legacyC, legacyD) = ChoiceOptions.ToLegacy(rootOptions);

        var question = new BankQuestion
        {
            Id = Guid.NewGuid(),
            CourseId = command.CourseId,
            LessonId = command.LessonId,
            CreatedByUserId = command.TeacherUserId,
            QuestionType = type,
            Prompt = command.Prompt.Trim(),
            PassageText = (command.PassageText ?? string.Empty).Trim(),
            OptionA = legacyA,
            OptionB = legacyB,
            OptionC = legacyC,
            OptionD = legacyD,
            OptionsJson = ChoiceOptions.ToJson(rootOptions),
            CorrectAnswer = type == BankQuestionType.MultiChoice
                ? string.Join(',', ExamGrading.NormalizeMultiAnswer(command.CorrectAnswer ?? string.Empty))
                : (command.CorrectAnswer ?? string.Empty).Trim(),
            Points = command.Points <= 0 ? 1 : command.Points,
            SortOrder = command.SortOrder <= 0 ? 1 : command.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (BankQuestionValidator.IsComposite(type))
        {
            var childOrder = 1;
            var childPoints = 0;
            foreach (var child in command.Children!)
            {
                var childType = BankQuestionValidator.ParseType(child.QuestionType);
                if (BankQuestionValidator.IsComposite(childType) || childType == BankQuestionType.Underline)
                {
                    throw new InvalidOperationException("Child questions cannot be Paragraph or Underline.");
                }

                BankQuestionValidator.ValidateLeaf(
                    childType,
                    child.Prompt,
                    child.OptionA,
                    child.OptionB,
                    child.OptionC,
                    child.OptionD,
                    child.CorrectAnswer,
                    options: child.Options);

                var childOptions = ResolveOptions(childType, child.Options, child.OptionA, child.OptionB, child.OptionC, child.OptionD);
                var (cA, cB, cC, cD) = ChoiceOptions.ToLegacy(childOptions);
                var points = child.Points <= 0 ? 1 : child.Points;
                childPoints += points;
                question.Children.Add(new BankQuestion
                {
                    Id = Guid.NewGuid(),
                    CourseId = command.CourseId,
                    LessonId = command.LessonId,
                    CreatedByUserId = command.TeacherUserId,
                    ParentQuestionId = question.Id,
                    QuestionType = childType,
                    Prompt = child.Prompt.Trim(),
                    PassageText = string.Empty,
                    OptionA = cA,
                    OptionB = cB,
                    OptionC = cC,
                    OptionD = cD,
                    OptionsJson = ChoiceOptions.ToJson(childOptions),
                    CorrectAnswer = childType == BankQuestionType.MultiChoice
                        ? string.Join(',', ExamGrading.NormalizeMultiAnswer(child.CorrectAnswer))
                        : child.CorrectAnswer.Trim(),
                    Points = points,
                    SortOrder = child.SortOrder <= 0 ? childOrder : child.SortOrder,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                childOrder++;
            }

            question.Points = childPoints;
            question.CorrectAnswer = string.Empty;
        }

        dbContext.BankQuestions.Add(question);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadDto(dbContext, question.Id, cancellationToken))!;
    }

    private static IReadOnlyList<ChoiceOptionDto> ResolveOptions(
        BankQuestionType type,
        IReadOnlyList<string>? options,
        string? optionA,
        string? optionB,
        string? optionC,
        string? optionD)
    {
        if (type is not (BankQuestionType.Choose or BankQuestionType.SingleChoice or BankQuestionType.MultiChoice))
        {
            return [];
        }

        return options is { Count: > 0 }
            ? ChoiceOptions.FromTexts(options)
            : ChoiceOptions.Parse(null, optionA, optionB, optionC, optionD);
    }

    internal static async Task EnsureTeacherCanUseCourse(
        IAppDbContext dbContext,
        Guid teacherUserId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        _ = await dbContext.Courses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var ownsClassroom = await dbContext.Classrooms.AnyAsync(
            x => x.TeacherId == teacherUserId && (x.CourseId == null || x.CourseId == courseId),
            cancellationToken);
        if (!ownsClassroom)
        {
            // Allow any teacher with at least one classroom to contribute to any course bank.
            ownsClassroom = await dbContext.Classrooms.AnyAsync(x => x.TeacherId == teacherUserId, cancellationToken);
        }

        if (!ownsClassroom)
        {
            throw new InvalidOperationException("Only assigned teachers can add bank questions.");
        }
    }

    internal static async Task<BankQuestionDto?> LoadDto(
        IAppDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var question = await dbContext.BankQuestions
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Lesson)
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return question is null ? null : Map(question);
    }

    internal static BankQuestionDto Map(BankQuestion q)
    {
        var options = ChoiceOptions.Parse(q.OptionsJson, q.OptionA, q.OptionB, q.OptionC, q.OptionD);
        return new(
            q.Id,
            q.CourseId,
            q.Course?.Title ?? "Course",
            q.LessonId,
            q.Lesson?.Title,
            q.CreatedByUserId,
            q.ParentQuestionId,
            q.QuestionType.ToString(),
            q.Prompt,
            q.PassageText,
            q.OptionA,
            q.OptionB,
            q.OptionC,
            q.OptionD,
            options,
            q.CorrectAnswer,
            q.Points,
            q.SortOrder,
            q.Children
                .OrderBy(c => c.SortOrder)
                .Select(Map)
                .ToList());
    }
}

public sealed class ListBankQuestionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListBankQuestionsQuery, IReadOnlyList<BankQuestionDto>>
{
    public async Task<IReadOnlyList<BankQuestionDto>> Handle(ListBankQuestionsQuery query, CancellationToken cancellationToken)
    {
        var items = await dbContext.BankQuestions
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Lesson)
            .Include(x => x.Children)
            .Where(x => x.ParentQuestionId == null && x.CreatedByUserId == query.TeacherUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.CourseId is Guid courseId)
        {
            items = items.Where(x => x.CourseId == courseId).ToList();
        }

        if (query.LessonId is Guid lessonId)
        {
            items = items.Where(x => x.LessonId == lessonId).ToList();
        }

        return items.Select(CreateBankQuestionCommandHandler.Map).ToList();
    }
}

public sealed class UpdateBankQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateBankQuestionCommand, BankQuestionDto>
{
    public async Task<BankQuestionDto> Handle(UpdateBankQuestionCommand command, CancellationToken cancellationToken)
    {
        var question = await dbContext.BankQuestions
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == command.QuestionId, cancellationToken)
            ?? throw new InvalidOperationException("Bank question not found.");

        if (question.CreatedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("You can only edit your own bank questions.");
        }

        if (question.ParentQuestionId is not null)
        {
            throw new InvalidOperationException("Edit the parent Paragraph question instead.");
        }

        BankQuestionValidator.ValidateLeaf(
            question.QuestionType,
            command.Prompt,
            command.OptionA,
            command.OptionB,
            command.OptionC,
            command.OptionD,
            command.CorrectAnswer ?? string.Empty,
            command.PassageText,
            command.Options);

        question.Prompt = command.Prompt.Trim();
        question.PassageText = (command.PassageText ?? string.Empty).Trim();
        question.LessonId = command.LessonId;

        var resolved = question.QuestionType is BankQuestionType.Choose
            or BankQuestionType.SingleChoice
            or BankQuestionType.MultiChoice
            ? (command.Options is { Count: > 0 }
                ? ChoiceOptions.FromTexts(command.Options)
                : ChoiceOptions.Parse(null, command.OptionA, command.OptionB, command.OptionC, command.OptionD))
            : Array.Empty<ChoiceOptionDto>();

        var (legacyA, legacyB, legacyC, legacyD) = ChoiceOptions.ToLegacy(resolved);
        question.OptionA = legacyA;
        question.OptionB = legacyB;
        question.OptionC = legacyC;
        question.OptionD = legacyD;
        question.OptionsJson = ChoiceOptions.ToJson(resolved);
        question.CorrectAnswer = question.QuestionType == BankQuestionType.MultiChoice
            ? string.Join(',', ExamGrading.NormalizeMultiAnswer(command.CorrectAnswer ?? string.Empty))
            : BankQuestionValidator.IsComposite(question.QuestionType)
                ? string.Empty
                : (command.CorrectAnswer ?? string.Empty).Trim();
        if (!BankQuestionValidator.IsComposite(question.QuestionType))
        {
            question.Points = command.Points <= 0 ? 1 : command.Points;
        }

        question.SortOrder = command.SortOrder <= 0 ? question.SortOrder : command.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateBankQuestionCommandHandler.LoadDto(dbContext, question.Id, cancellationToken))!;
    }
}

public sealed class DeleteBankQuestionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteBankQuestionCommand, bool>
{
    public async Task<bool> Handle(DeleteBankQuestionCommand command, CancellationToken cancellationToken)
    {
        var question = await dbContext.BankQuestions
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == command.QuestionId, cancellationToken)
            ?? throw new InvalidOperationException("Bank question not found.");

        if (question.CreatedByUserId != command.TeacherUserId)
        {
            throw new InvalidOperationException("You can only delete your own bank questions.");
        }

        if (question.ParentQuestionId is not null)
        {
            throw new InvalidOperationException("Delete the parent Paragraph question instead.");
        }

        dbContext.BankQuestions.RemoveRange(question.Children);
        dbContext.BankQuestions.Remove(question);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
