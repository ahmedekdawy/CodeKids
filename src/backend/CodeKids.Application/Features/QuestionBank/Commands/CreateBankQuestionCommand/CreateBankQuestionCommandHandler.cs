using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

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
            x => x.Courses.Any(t => t.TeacherId == teacherUserId) && (x.CourseId == null || x.CourseId == courseId),
            cancellationToken);
        if (!ownsClassroom)
        {
            // Allow any teacher with at least one classroom to contribute to any course bank.
            ownsClassroom = await dbContext.ClassroomCourses.AnyAsync(
                x => x.TeacherId == teacherUserId,
                cancellationToken);
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
