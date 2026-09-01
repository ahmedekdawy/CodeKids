using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class DeleteQuizCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteQuizCommand, bool>
{
    public async Task<bool> Handle(DeleteQuizCommand command, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        QuizAuthorization.EnsureCanManage(quiz, command.TeacherUserId);

        var attempts = await dbContext.QuizAttempts
            .Include(x => x.Answers)
            .Where(x => x.QuizId == quiz.Id)
            .ToListAsync(cancellationToken);

        dbContext.QuizAttemptAnswers.RemoveRange(attempts.SelectMany(x => x.Answers));
        dbContext.QuizAttempts.RemoveRange(attempts);
        dbContext.QuizQuestions.RemoveRange(quiz.Questions);
        dbContext.Quizzes.Remove(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
