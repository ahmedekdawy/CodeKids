using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class SubmitQuizCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitQuizCommand, SubmitQuizResponse>
{
    public async Task<SubmitQuizResponse> Handle(SubmitQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        if (!quiz.IsPublished)
        {
            throw new InvalidOperationException("Quiz is not available.");
        }

        var answerable = quiz.Questions
            .Where(x => x.QuestionType != BankQuestionType.Paragraph)
            .ToList();
        var score = 0;
        foreach (var question in answerable)
        {
            var answer = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var selected = answer?.SelectedOption?.Trim() ?? string.Empty;
            if (question.QuestionType == BankQuestionType.MultiChoice)
            {
                selected = string.Join(',', ExamGrading.NormalizeMultiAnswer(selected));
            }

            var correct = string.IsNullOrWhiteSpace(question.CorrectAnswer)
                ? question.CorrectOption
                : question.CorrectAnswer;
            if (ExamGrading.AnswersMatch(question.QuestionType, selected, correct))
            {
                score++;
            }
        }

        var total = answerable.Count;
        var ratio = total == 0 ? 0 : (double)score / total;
        var earnedXp = ratio >= 0.7 ? quiz.XpReward : Math.Max(5, quiz.XpReward / 3);

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuizId = quiz.Id,
            Score = score,
            TotalQuestions = total,
            EarnedXp = earnedXp,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var question in answerable.OrderBy(x => x.SortOrder))
        {
            var answer = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var selected = answer?.SelectedOption?.Trim() ?? string.Empty;
            if (question.QuestionType == BankQuestionType.MultiChoice)
            {
                selected = string.Join(',', ExamGrading.NormalizeMultiAnswer(selected));
            }

            var correct = string.IsNullOrWhiteSpace(question.CorrectAnswer)
                ? question.CorrectOption
                : question.CorrectAnswer;
            attempt.Answers.Add(new QuizAttemptAnswer
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                SelectedOption = selected,
                IsCorrect = ExamGrading.AnswersMatch(question.QuestionType, selected, correct)
            });
        }

        dbContext.QuizAttempts.Add(attempt);

        user.TotalXp += earnedXp;
        await dbContext.SaveChangesAsync(cancellationToken);

        var beforeBadges = await dbContext.UserBadges
            .Where(x => x.UserId == user.Id)
            .Select(x => x.BadgeId)
            .ToListAsync(cancellationToken);

        await BadgeAwarder.AwardEligibleAsync(dbContext, user, cancellationToken);

        var newBadges = await dbContext.UserBadges
            .Include(x => x.Badge)
            .Where(x => x.UserId == user.Id && !beforeBadges.Contains(x.BadgeId))
            .Select(x => x.Badge!.Name)
            .ToListAsync(cancellationToken);

        return new SubmitQuizResponse(
            score,
            total,
            earnedXp,
            user.TotalXp,
            ratio >= 0.7 ? "Quiz cleared! Your coding brain is glowing." : "Keep practicing — you're getting closer!",
            ratio >= 0.7 ? "api.feedback.quizPassed" : "api.feedback.quizRetry",
            newBadges);
    }
}
