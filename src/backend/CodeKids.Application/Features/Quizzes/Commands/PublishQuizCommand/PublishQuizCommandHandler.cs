using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Notifications;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class PublishQuizCommandHandler(IAppDbContext dbContext, NotificationPublisher notifications)
    : ICommandHandler<PublishQuizCommand, QuizDto>
{
    public async Task<QuizDto> Handle(PublishQuizCommand command, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        QuizAuthorization.EnsureCanManage(quiz, command.TeacherUserId);

        var wasPublished = quiz.IsPublished;
        quiz.IsPublished = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!wasPublished)
        {
            await notifications.NotifyQuizCreatedAsync(quiz, cancellationToken);
        }

        return GetQuizzesQueryHandler.Map(quiz);
    }
}
