using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public static class QuizQuestionSync
{
    public static async Task ApplyAsync(
        IAppDbContext dbContext,
        Quiz quiz,
        IReadOnlyList<CreateQuizQuestionInput> incoming,
        CancellationToken cancellationToken)
    {
        var incomingList = incoming.ToList();
        var keptIds = TypedQuestionSupport.FlattenIds(
                incomingList,
                q => q.Id,
                q => q.Children)
            .ToHashSet();

        var removed = quiz.Questions.Where(q => !keptIds.Contains(q.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(q => q.Id).ToHashSet();
            var answers = await dbContext.QuizAttemptAnswers
                .Where(a => removedIds.Contains(a.QuestionId))
                .ToListAsync(cancellationToken);
            dbContext.QuizAttemptAnswers.RemoveRange(answers);

            foreach (var child in removed.Where(q => q.ParentQuestionId is not null).ToList())
            {
                quiz.Questions.Remove(child);
                dbContext.QuizQuestions.Remove(child);
            }

            foreach (var root in removed.Where(q => q.ParentQuestionId is null).ToList())
            {
                quiz.Questions.Remove(root);
                dbContext.QuizQuestions.Remove(root);
            }
        }

        var order = 1;
        foreach (var input in incomingList)
        {
            await UpsertAsync(dbContext, quiz, input, parentId: null, order, cancellationToken);
            order++;
        }
    }

    public static IReadOnlyList<QuizQuestionDto> MapTree(IEnumerable<QuizQuestion> questions) =>
        MapTree<QuizQuestionDto>(questions, MapStudent);

    public static IReadOnlyList<TeacherQuizQuestionDetailDto> MapTeacherTree(IEnumerable<QuizQuestion> questions)
    {
        var list = questions.ToList();
        var children = ChildrenByParent(list);

        TeacherQuizQuestionDetailDto MapOne(QuizQuestion q)
        {
            var options = ChoiceOptions.Parse(q.OptionsJson, q.OptionA, q.OptionB, q.OptionC);
            var nested = children.TryGetValue(q.Id, out var kids)
                ? kids.Select(MapOne).ToList()
                : [];
            var correct = string.IsNullOrWhiteSpace(q.CorrectAnswer) ? q.CorrectOption : q.CorrectAnswer;
            return new TeacherQuizQuestionDetailDto(
                q.Id,
                q.Prompt,
                q.QuestionType.ToString(),
                q.PassageText,
                options,
                q.CorrectOption,
                correct,
                q.Points,
                q.SortOrder,
                q.PromptImageMediaAssetId,
                QuestionImageUrls.Build(q.PromptImageMediaAssetId),
                nested);
        }

        return Roots(list).Select(MapOne).ToList();
    }

    private static QuizQuestionDto MapStudent(QuizQuestion q, IReadOnlyList<QuizQuestionDto> children)
    {
        var options = ChoiceOptions.Parse(q.OptionsJson, q.OptionA, q.OptionB, q.OptionC);
        return new QuizQuestionDto(
            q.Id,
            q.Prompt,
            q.QuestionType.ToString(),
            q.PassageText,
            q.OptionA,
            q.OptionB,
            q.OptionC,
            options,
            q.SortOrder,
            QuestionImageUrls.Build(q.PromptImageMediaAssetId),
            children);
    }

    private static IReadOnlyList<T> MapTree<T>(
        IEnumerable<QuizQuestion> questions,
        Func<QuizQuestion, IReadOnlyList<T>, T> map)
    {
        var list = questions.ToList();
        var children = ChildrenByParent(list);

        T MapOne(QuizQuestion q)
        {
            var nested = children.TryGetValue(q.Id, out var kids)
                ? kids.Select(MapOne).ToList()
                : [];
            return map(q, nested);
        }

        return Roots(list).Select(MapOne).ToList();
    }

    private static Dictionary<Guid, List<QuizQuestion>> ChildrenByParent(List<QuizQuestion> list) =>
        list.Where(q => q.ParentQuestionId is not null)
            .GroupBy(q => q.ParentQuestionId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ToList());

    private static IEnumerable<QuizQuestion> Roots(List<QuizQuestion> list) =>
        list.Where(q => q.ParentQuestionId is null).OrderBy(q => q.SortOrder);

    private static async Task<QuizQuestion> UpsertAsync(
        IAppDbContext dbContext,
        Quiz quiz,
        CreateQuizQuestionInput input,
        Guid? parentId,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var type = TypedQuestionSupport.ParseQuizType(input.QuestionType);
        var children = input.Children ?? [];
        var correct = string.IsNullOrWhiteSpace(input.CorrectAnswer)
            ? (input.CorrectOption ?? string.Empty)
            : input.CorrectAnswer;

        TypedQuestionSupport.ValidateQuiz(
            type,
            input.Prompt,
            input.OptionA,
            input.OptionB,
            input.OptionC,
            correct,
            input.PassageText,
            input.Options,
            children.Select(c => new QuizChildSpec(
                c.Prompt,
                c.QuestionType ?? nameof(BankQuestionType.SingleChoice),
                c.OptionA,
                c.OptionB,
                c.OptionC,
                c.Options,
                string.IsNullOrWhiteSpace(c.CorrectAnswer) ? (c.CorrectOption ?? string.Empty) : c.CorrectAnswer!,
                c.Points,
                c.SortOrder,
                c.PromptImageMediaAssetId,
                c.Id)).ToList());

        await TypedQuestionSupport.EnsureImageAsync(dbContext, input.PromptImageMediaAssetId, cancellationToken);

        var existing = input.Id is Guid id
            ? quiz.Questions.FirstOrDefault(x => x.Id == id)
            : null;
        var entity = existing ?? new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id
        };

        var options = TypedQuestionSupport.ResolveOptions(type, input.Options, input.OptionA, input.OptionB, input.OptionC);
        var (legacyA, legacyB, legacyC, _) = ChoiceOptions.ToLegacy(options);
        var normalized = TypedQuestionSupport.NormalizeCorrect(type, correct);

        entity.ParentQuestionId = parentId;
        entity.QuestionType = type;
        entity.Prompt = input.Prompt.Trim();
        entity.PassageText = (input.PassageText ?? string.Empty).Trim();
        entity.OptionA = legacyA ?? string.Empty;
        entity.OptionB = legacyB ?? string.Empty;
        entity.OptionC = legacyC ?? string.Empty;
        entity.OptionsJson = ChoiceOptions.ToJson(options);
        entity.CorrectAnswer = normalized;
        entity.CorrectOption = string.IsNullOrWhiteSpace(normalized) ? "A" : normalized;
        entity.Points = input.Points <= 0 ? 1 : input.Points;
        entity.SortOrder = input.SortOrder <= 0 ? sortOrder : input.SortOrder;
        entity.PromptImageMediaAssetId = input.PromptImageMediaAssetId ?? existing?.PromptImageMediaAssetId;

        if (existing is null)
        {
            quiz.Questions.Add(entity);
        }

        if (BankQuestionValidator.IsComposite(type))
        {
            var childOrder = 1;
            var childPoints = 0;
            foreach (var child in children)
            {
                var childEntity = await UpsertAsync(
                    dbContext,
                    quiz,
                    child,
                    entity.Id,
                    childOrder,
                    cancellationToken);
                childPoints += childEntity.Points;
                childOrder++;
            }

            entity.Points = childPoints;
            entity.CorrectAnswer = string.Empty;
            entity.CorrectOption = "A";
            entity.OptionsJson = ChoiceOptions.ToJson([]);
            entity.OptionA = string.Empty;
            entity.OptionB = string.Empty;
            entity.OptionC = string.Empty;
        }

        return entity;
    }
}
