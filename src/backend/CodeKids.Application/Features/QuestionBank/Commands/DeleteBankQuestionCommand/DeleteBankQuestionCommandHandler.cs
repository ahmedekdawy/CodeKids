using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

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
