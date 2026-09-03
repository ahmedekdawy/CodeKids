using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Assignments;

public static class AssignmentQuestionSync
{
    public static async Task ApplyAsync(
        IAppDbContext dbContext,
        Assignment assignment,
        IReadOnlyList<AssignmentQuestionInput> incoming,
        CancellationToken cancellationToken)
    {
        var incomingList = incoming.ToList();
        var keptIds = TypedQuestionSupport.FlattenIds(
                incomingList,
                q => q.Id,
                q => q.Children)
            .ToHashSet();

        var removed = assignment.Questions.Where(q => !keptIds.Contains(q.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(q => q.Id).ToHashSet();
            var answers = assignment.Submissions
                .SelectMany(s => s.Answers)
                .Where(a => removedIds.Contains(a.QuestionId))
                .ToList();
            dbContext.AssignmentAnswers.RemoveRange(answers);

            foreach (var child in removed.Where(q => q.ParentQuestionId is not null).ToList())
            {
                assignment.Questions.Remove(child);
                dbContext.AssignmentQuestions.Remove(child);
            }

            foreach (var root in removed.Where(q => q.ParentQuestionId is null).ToList())
            {
                assignment.Questions.Remove(root);
                dbContext.AssignmentQuestions.Remove(root);
            }
        }

        var order = 1;
        foreach (var input in incomingList)
        {
            await UpsertAsync(dbContext, assignment, input, parentId: null, order, cancellationToken);
            order++;
        }
    }

    public static IReadOnlyList<AssignmentQuestionDto> MapTree(
        IEnumerable<AssignmentQuestion> questions,
        bool includeAnswerKey)
    {
        var list = questions.ToList();
        var children = list
            .Where(q => q.ParentQuestionId is not null)
            .GroupBy(q => q.ParentQuestionId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ToList());

        AssignmentQuestionDto MapOne(AssignmentQuestion q)
        {
            var options = ChoiceOptions.Parse(q.OptionsJson, q.OptionA, q.OptionB, q.OptionC);
            var nested = children.TryGetValue(q.Id, out var kids)
                ? kids.Select(MapOne).ToList()
                : [];
            return new AssignmentQuestionDto(
                q.Id,
                q.Prompt,
                q.QuestionType.ToString(),
                q.PassageText,
                q.OptionA,
                q.OptionB,
                q.OptionC,
                options,
                q.Points,
                q.SortOrder,
                includeAnswerKey ? q.CorrectAnswer : null,
                QuestionImageUrls.Build(q.PromptImageMediaAssetId),
                q.PromptImageMediaAssetId,
                nested);
        }

        return list
            .Where(q => q.ParentQuestionId is null)
            .OrderBy(q => q.SortOrder)
            .Select(MapOne)
            .ToList();
    }

    private static async Task<AssignmentQuestion> UpsertAsync(
        IAppDbContext dbContext,
        Assignment assignment,
        AssignmentQuestionInput input,
        Guid? parentId,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var type = TypedQuestionSupport.ParseAssignmentType(input.QuestionType);
        var children = input.Children ?? [];
        TypedQuestionSupport.ValidateAssignment(
            type,
            input.Prompt,
            input.OptionA,
            input.OptionB,
            input.OptionC,
            input.CorrectAnswer ?? string.Empty,
            input.PassageText,
            input.Options,
            children.Select(c => new AssignmentChildSpec(
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
                c.Id)).ToList());

        await TypedQuestionSupport.EnsureImageAsync(dbContext, input.PromptImageMediaAssetId, cancellationToken);

        var existing = input.Id is Guid id
            ? assignment.Questions.FirstOrDefault(x => x.Id == id)
            : null;
        var entity = existing ?? new AssignmentQuestion
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id
        };

        var bankType = TypedQuestionSupport.IsShortAnswer(type)
            ? (BankQuestionType?)null
            : TypedQuestionSupport.ToBankType(type);
        var options = bankType is BankQuestionType resolved
            ? TypedQuestionSupport.ResolveOptions(resolved, input.Options, input.OptionA, input.OptionB, input.OptionC)
            : [];
        var (legacyA, legacyB, legacyC, _) = ChoiceOptions.ToLegacy(options);

        entity.ParentQuestionId = parentId;
        entity.Prompt = input.Prompt.Trim();
        entity.QuestionType = type;
        entity.PassageText = (input.PassageText ?? string.Empty).Trim();
        entity.OptionA = legacyA;
        entity.OptionB = legacyB;
        entity.OptionC = legacyC;
        entity.OptionsJson = ChoiceOptions.ToJson(options);
        entity.CorrectAnswer = TypedQuestionSupport.IsFreeText(type)
            ? string.Empty
            : bankType is BankQuestionType bt
                ? TypedQuestionSupport.NormalizeCorrect(bt, input.CorrectAnswer)
                : (input.CorrectAnswer ?? string.Empty).Trim();
        entity.Points = input.Points <= 0 ? 1 : input.Points;
        entity.SortOrder = input.SortOrder <= 0 ? sortOrder : input.SortOrder;
        entity.PromptImageMediaAssetId = input.PromptImageMediaAssetId ?? existing?.PromptImageMediaAssetId;

        if (existing is null)
        {
            assignment.Questions.Add(entity);
        }

        if (TypedQuestionSupport.IsComposite(type))
        {
            var childOrder = 1;
            var childPoints = 0;
            foreach (var child in children)
            {
                var childEntity = await UpsertAsync(
                    dbContext,
                    assignment,
                    child,
                    entity.Id,
                    childOrder,
                    cancellationToken);
                childPoints += childEntity.Points;
                childOrder++;
            }

            entity.Points = childPoints;
            entity.CorrectAnswer = string.Empty;
            entity.OptionsJson = ChoiceOptions.ToJson([]);
            entity.OptionA = null;
            entity.OptionB = null;
            entity.OptionC = null;
        }

        return entity;
    }
}
