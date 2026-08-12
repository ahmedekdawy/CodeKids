using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
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
