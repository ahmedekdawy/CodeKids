using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

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
            command.Options,
            command.MapMarkers);

        if (question.QuestionType == BankQuestionType.Map)
        {
            var imageId = command.PromptImageMediaAssetId ?? question.PromptImageMediaAssetId;
            if (imageId is null)
            {
                throw new InvalidOperationException("Map questions require a map image.");
            }

            await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, imageId, cancellationToken);
            question.PromptImageMediaAssetId = imageId;
        }

        question.Prompt = command.Prompt.Trim();
        question.PassageText = (command.PassageText ?? string.Empty).Trim();
        question.LessonId = command.LessonId;

        if (question.QuestionType == BankQuestionType.Map)
        {
            var markers = MapMarkers.Normalize(command.MapMarkers);
            question.OptionA = null;
            question.OptionB = null;
            question.OptionC = null;
            question.OptionD = null;
            question.OptionsJson = MapMarkers.ToJson(markers);
        }
        else
        {
            var resolved = question.QuestionType is BankQuestionType.Choose
                or BankQuestionType.SingleChoice
                or BankQuestionType.MultiChoice
                or BankQuestionType.Order
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
        }

        question.CorrectAnswer = BankQuestionValidator.IsComposite(question.QuestionType)
            ? string.Empty
            : question.QuestionType == BankQuestionType.Complete
                ? CompleteBlanks.Join(CompleteBlanks.Extract(command.PassageText))
                : TypedQuestionSupport.NormalizeCorrect(question.QuestionType, command.CorrectAnswer);
        if (!BankQuestionValidator.IsComposite(question.QuestionType))
        {
            question.Points = command.Points <= 0 ? 1 : command.Points;
        }

        question.SortOrder = command.SortOrder <= 0 ? question.SortOrder : command.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateBankQuestionCommandHandler.LoadDto(dbContext, question.Id, cancellationToken))!;
    }
}
